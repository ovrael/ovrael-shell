# MakeWidget101 — how to add a widget to OvraelShell

A practical guide based on the widgets that already exist (Network, Audio, Bluetooth,
Weather, Workspaces, Calendar). Read it before you add a new widget. Use the checklist at
the end before you commit.

Each section names a file that already does the thing. When in doubt, open that file and
copy its shape.

---

## 1. The big picture

```
Program.cs
 ├─ UserSettings.Load()                    static, lives for the whole app
 ├─ XxxService  (one per application)      owns the data, talks to the system
 └─ MainBar     (one per Activate/monitor) owns the widgets
     └─ XxxBarButton                        the button on the bar
         └─ XxxPopover                      opens on click
             └─ Elements/...                pages, lists, rows
```

The most important rule in this project comes from this picture:

> **Services and settings live longer than widgets.**
> A widget that subscribes to a service event is referenced *by the service*.
> If the widget does not unsubscribe, the service keeps it (and the whole bar) alive.

Almost every "how do I…" in this guide comes back to this rule.

### Folder layout

| What | Where | Example |
|---|---|---|
| Service | `Services/XxxService.cs` | `Services/WeatherService.cs` |
| Data models | `Models/Xxx/` | `Models/Weather/WeatherForecast.cs` |
| Bar button + popover | `Widgets/Xxx/` | `Widgets/Bluetooth/` |
| Popover parts (pages, lists, rows) | `Widgets/Xxx/Elements/` | `Widgets/Bluetooth/Elements/` |
| Simple bar widget without popover | `Widgets/BarButtons/` | `CalendarWidget.cs`, `WorkspaceWidget.cs` |
| Settings section | `Models/Settings/Xxx/` | `Models/Settings/Weather/` |
| System bus / socket helpers | `Utils/`, own folder for a protocol | `Utils/BluezBus.cs`, `Hyprland/` |
| Icons (Nerd Font) and cursors | `InterfaceElements/` | `Icons.cs`, `Cursors.cs` |
| Styles | `Styles/Shared`, `Styles/Themed` | `Styles/Themed/020_Main.gtk.css` |

---

## 2. Step by step

### Step 1 — the model

Models hold state and tell others when it changes. Use the two reactive types from `Models/`:

- **`ReactiveProperty<T>`**: one value. `Set` sends `Changed` **only when the value is different**.
  `Set` returns `true` when it changed the value.
- **`ReactiveCollection<T>`**: a list with the events `Added`, `Removed`, `Cleared` and `Changed`.
  `Sync(keys, keyOf, create)` adds the missing items and removes the old ones. Items with the
  same key keep their object.

```csharp
public ReactiveProperty<int?> Level { get; } = new();

// A list value - give a comparer, or every Set is a "change" (new list instance)
public ReactiveProperty<IReadOnlyList<WeatherData>> Hourly { get; } =
    new([], SequenceComparer<WeatherData>.Instance);
```

Rules:

- **Use records for value objects** (`WeatherData`). Equality by value means that the same
  data from a new response does not send `Changed`.
- **Give list values a comparer** (`SequenceComparer<T>.Instance`).
- **Use a key that does not change.** Use a D-Bus object path or coordinates as the key,
  never a display name. A name can change (for example, geocoding finds it later).
- An object that goes into a GTK model (`Gio.ListStore`) must be a GObject:
  `[GObject.Subclass<GObject.Object>]`. Override `Equals`/`GetHashCode` with the stable
  identity. See `WeatherForecast`, `BluetoothDevice`.
- If widgets must follow "anything changed" on one object, add one aggregate event
  (`IReactivable<T>.OnChange`). The object can subscribe to its *own* properties with lambdas,
  because they have the same lifetime:

  ```csharp
  partial void Initialize()
  {
      Name.Changed += (_) => OnChange?.Invoke(this);
      Current.Changed += (_) => OnChange?.Invoke(this);
  }
  ```

  Note: one response that sets 3 properties sends `OnChange` 3 times. Keep the handlers cheap.

- `ReactiveCollection.Clear()` sends **`Cleared`, not `Removed` for each item**. A subscriber
  that keeps its own state per item must handle `Cleared` too (see `WeatherPopover.ClearLocations`,
  `PipeWireNodeList`).

### Step 2 — the service

A service is a plain `sealed class XxxService : IDisposable`. It is created once and shared
by all bars.

The skeleton (based on `WeatherService` and `HyprlandService`):

```csharp
/// <summary>What it follows. The values change on the main thread.</summary>
public sealed class XxxService : IDisposable
{
    private const uint INTERVAL_MS = 60_000;

    private uint timeoutId;
    private CancellationTokenSource? updateCancellation;

    public ReactiveProperty<XxxState?> State { get; } = new();

    public XxxService()
    {
        // The service lives as long as the application, like the static settings
        UserSettings.Xxx.Something.Changed += OnSomethingChanged;

        RestartUpdate();
        timeoutId = GLib.Functions.TimeoutAdd(0, INTERVAL_MS, () =>
        {
            RestartUpdate();
            return true; // keep the timeout
        });
    }

    // A new request replaces the running one - its input can be out of date
    private void RestartUpdate()
    {
        updateCancellation?.Cancel();
        updateCancellation = new CancellationTokenSource();
        _ = UpdateAsync(updateCancellation.Token);
    }

    private async Task UpdateAsync(CancellationToken token)
    {
        try
        {
            var data = await FetchAsync(token);       // does not block the main loop
            if (token.IsCancellationRequested)
                return;

            State.Set(data);                          // back on the main thread, see section 4
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Replaced by a newer request, or disposed
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            Console.WriteLine($"Xxx: cannot update: {ex.Message}");
        }
    }

    public void Dispose()
    {
        UserSettings.Xxx.Something.Changed -= OnSomethingChanged;

        updateCancellation?.Cancel();
        if (timeoutId != 0)
            GLib.Functions.SourceRemove(timeoutId);
    }
}
```

What a service must do:

- **Change reactive values only on the main thread.** Widgets subscribe to them, and GTK is
  single-threaded.
- **Never block the main loop.** See section 4.
- **Undo in `Dispose` everything that it started**: event subscriptions, GLib sources
  (`SourceRemove`), cancellation tokens, `HttpClient`, `FileMonitor`, D-Bus signal handlers.
- **Log and continue.** A missing daemon (BlueZ not running, no Hyprland socket) is a normal
  state. The service sets "nothing" (`null`, empty collection) and the widget shows that.
- **Start again when the daemon comes back**, if it can come and go. See `BluetoothService`:
  `g-name-owner` notify → `Reload()`.

### Step 3 — register the service in `Program.cs`

```csharp
XxxService? xxxService = null;                     // with the other services

application.OnActivate += (sender, e) =>
{
    UserSettings.Load();                           // before the services and bars
    ...
    // Activate runs again when the application is launched a second time
    xxxService ??= new XxxService();
    CreateBar(sender, ..., xxxService);
};

application.OnShutdown += (_, _) =>
{
    foreach (var bar in bars)
        bar.RemoveServices();                      // widgets first
    ...
    xxxService?.Dispose();                         // then the services
};
```

- Use `??=`. `Activate` runs again when you start the app a second time. A second `new` makes
  a second service that nobody disposes.
- Detach the widgets **before** you dispose the services. If you do it in the other order,
  disposing a service can update widgets that are being destroyed.

### Step 4 — the bar button

The project uses GirCore subclasses. Know how they are built:

- `[GObject.Subclass<Gtk.Box>]` generates `NewWithProperties([])` and calls your
  `partial void Initialize()` **inside the constructor**, before your `New` can set any field.
- That is why the shape is always:
  1. `Initialize()`: build the UI that does not need the service.
  2. `New(service)`: call `NewWithProperties([])`, then `AddService(service)`, then everything
     that needs the service (for example the popover).
- Mark the methods that set non-nullable fields with `[MemberNotNull(nameof(...))]`.
  The generator still reports CS8618 for fields set in `New`. This is a known and accepted
  warning in this project.

Template (from `WeatherBarButton`, `BluetoothBarButton`):

```csharp
[GObject.Subclass<Gtk.Box>]
public sealed partial class XxxBarButton : IWithDisposableService<XxxService>
{
    public XxxService Service { get; private set; }
    private Button button;
    private XxxPopover popover;

    public static XxxBarButton New(XxxService service)
    {
        var widget = NewWithProperties([]);
        widget.AddService(service);
        widget.InitPopover(service);
        return widget;
    }

    [MemberNotNull(nameof(button))]
    partial void Initialize()
    {
        SetOrientation(Orientation.Horizontal);

        button = Button.NewWithLabel(Icons.Xxx.Default);
        button.SetCursor(Cursors.Pointer);          // every clickable thing gets the pointer
        Append(button);
    }

    [MemberNotNull(nameof(Service))]
    public void AddService(XxxService service)
    {
        Service = service;
        Service.State.Changed += OnStateChanged;    // named method - it must be removable
        UpdateLabel();
    }

    // Safe to call more than once
    public void RemoveService()
    {
        Service.State.Changed -= OnStateChanged;
        popover.RemoveService();
    }

    [MemberNotNull(nameof(popover))]
    private void InitPopover(XxxService service)
    {
        popover = XxxPopover.New(service, button);
        button.OnClicked += (_, _) => popover.Popup();  // own child - a lambda is fine
    }

    private void OnStateChanged(XxxState? _) => UpdateLabel();
}
```

### Step 5 — the popover

Template: `WeatherPopover`, `BluetoothPopover`.

```csharp
[MemberNotNull(nameof(content))]
partial void Initialize()
{
    CreateContent();

    SetPosition(PositionType.Top);   // the bar is at the bottom
    SetAutohide(true);
    SetChild(content);
    AddCssClass("popover");

    OnShow += (_, _) => OnShown();
    OnHide += (_, _) => OnHidden();
}
```

Things that are easy to get wrong:

- **Size changes flicker on Hyprland.** Every size change of an open popover makes GTK move
  the `xdg_popup` again. If the content changes often (device discovery, live lists), give the
  scrolling area a fixed size when the popover opens. See `BluetoothPopover.UpdatePagesHeight`,
  which uses `MonitorSize.HeightOf`. A `Gtk.Stack` is homogeneous by default, so tabs of
  different height do not resize it.
- **Do the work only while it is open.** Start expensive things in `OnShow` and stop them in
  `OnHide`. For example, `BluetoothService.SetDiscovery(true/false)`.
- **Keyboard.** The bar has `KeyboardMode.None`. A popover with an entry must call
  `BarKeyboard.Request(this)` in `OnShow` and `BarKeyboard.Release(this)` in `OnHide`.
  If it does not release the keyboard, the bar keeps the focus after the popover closes.
- **Tabs**: `Stack` + `StackSwitcher`. Give the switcher a CSS class (`xxx-tabs`) and
  `SetCursor(Cursors.Pointer)`. The tab buttons inherit the cursor from the switcher.
  For a dynamic number of tabs, see `WeatherPopover` (one tab per item, the title follows a
  property, the tab bar scrolls when it is wider than the monitor).
- **Horizontal scrolling only**: a `ScrolledWindow` with `PolicyType.Never` vertically ignores
  the mouse wheel. Call `HorizontalScroll.AddMouseWheel(scroll)`.
- **Width follows the content**: `SetPropagateNaturalWidth(true)` lets the scroll ask for the
  width of its child. Use `SetMaxContentWidth` to limit it (for example to the monitor,
  `MonitorSize.WidthOf`).

### Step 6 — elements: pages, lists, rows

Two kinds:

**a) Plain C# classes that own a GTK widget**: `WeatherLocationPage`, `BluetoothDeviceList`.
Use them when you do not need a GObject subclass. Expose the widget as `View` and implement
`IDisposable`. `Dispose` removes the subscriptions. The owner calls `Dispose` when it removes the
element.

**b) Lists**: use `Gtk.ListView` with a `Gio.ListStore`, not a `Box` with many children.
Rows are recycled.

```
Gio.ListStore  →  SortListModel(CustomSorter)  →  FilterListModel(CustomFilter)
               →  NoSelection / SingleSelection  →  ListView(SignalListItemFactory)
```

See `BluetoothDeviceList`. Its rules:

- `OnSetup` creates the row widget **once**. `OnBind` connects it to an item. `OnUnbind`
  disconnects it.
- **A row must unsubscribe in `Unbind`.** A recycled row that still follows the old item
  updates itself with the wrong data and keeps the old item alive:

  ```csharp
  public void Bind(BluetoothDevice device)
  {
      this.device = device;
      device.OnChange += OnDeviceChanged;
      Refresh(device);
  }

  public void Unbind()
  {
      device?.OnChange -= OnDeviceChanged;
      device = null;
  }
  ```

- When a property that affects sorting or filtering changes, call `sorter.Changed(...)` or
  `filter.Changed(...)`. Subscribe to these properties when the item is added to the model,
  and unsubscribe when it is removed.
- `CustomSorter` gets raw pointers. Wrap them with
  `GObject.Internal.InstanceWrapper.WrapHandle<T>(handle, false)`.
- For a few small items that change rarely, it is simpler to remove all children and build
  them again (`WorkspaceWidget.UpdateLabels`, `WeatherLocationPage.FillRow`):

  ```csharp
  while (box.GetFirstChild() is { } child)
      box.Remove(child);
  ```

### Step 7 — put it on the bar (`MainBar.cs`)

1. Add the service field and the constructor parameter of `MainBar.New`, and pass it in
   `Program.CreateBar`.
2. Create the widget in `CreateStart` / `CreateCenter` / `CreateEnd`.
3. **If the widget implements `IWithDisposableService`, keep it in a field and call its
   `RemoveService()` in `MainBar.RemoveServices()`.** This is the only place where the bar
   detaches from the shared services: when it closes, when the monitor is unplugged, and when
   the app quits. A widget that is not in this list leaks with its whole bar.

### Step 8 — settings (optional)

Settings are in `settings.json`. The file is watched and reloaded on save. Every value is a
`ReactiveProperty`, so widgets react to edits without a restart.

To add a section:

1. **`Models/Settings/Xxx/XxxSection.cs`**: the shape of the JSON. Use nullable properties
   whose initial values are the defaults:

   ```csharp
   public class XxxSection
   {
       public bool? ShowLabel { get; set; } = true;
       public List<XxxEntry?>? Items { get; set; } = [];
   }
   ```

2. **`Models/Settings/Xxx/XxxSettings.cs`**: the validated, reactive values plus
   `internal void Apply(XxxSection section)`. When a value is incorrect, **log it and use the
   default**. Never throw. See `WeatherSettings` (validation of each entry, a limit on the
   number of entries, the entry index in log messages).
3. Add the section to `SettingsFile` and the property to `UserSettings`
   (`public static XxxSettings Xxx { get; } = new();`). Call `Xxx.Apply(...)` in
   `UserSettings.Reload`.
4. If the section needs a separate file next to `settings.json` (a cache that the user can
   edit), see `LocationNamesCache`. It uses the same JSON options, watches its own file,
   ignores its own saves, and does not save over a file that has a JSON error.

Lists from settings: use `SequenceComparer` for the property. Use records for the items, so
that a reload with the same content does not send `Changed`.

### Step 9 — styles

- `Styles/Shared/*.gtk.css`: layout that does not depend on the theme. Loaded once, priority
  *theme*.
- `Styles/ThemeColors/{Dark,Light}Colors.gtk.css`: only `@define-color`.
- `Styles/Themed/*.gtk.css`: everything that uses colors. Priority *user*. Reloaded when the
  theme changes. **Line 1 must be the `@import url(...)` of the colors.** The loader replaces
  it with the current theme.
- Use the color names (`@foreground`, `@button-background`, …), not hex values, in themed files.
- **The loader joins the lines without a newline.** Do not split a descendant selector over two
  lines. `.a\n.b {` becomes `.a.b {`, which is a different selector.
- Use your own CSS classes with a prefix (`.weather-…`, `.bluetooth-…`), and add them with
  `AddCssClass`. If a shared rule already exists, add your class to it (for example the tab
  rule for `.audio-tabs, .bluetooth-tabs, .weather-tabs`).

---

## 3. Reactivity rules

1. **The UI is a function of the state.** Write one `Update()`/`Refresh()` that reads the
   current values and sets the whole widget. Every event handler only calls it. Call it once
   at the end of `AddService`/`Bind` for the first state.
2. **Subscribe with named methods** when the publisher can live longer than you. You cannot
   remove a lambda.

   ```csharp
   Service.State.Changed += OnStateChanged;           // OK - removable
   Service.State.Changed += _ => UpdateLabel();       // leak - cannot be removed
   ```

   A lambda is fine only for your own children (`button.OnClicked`) or your own properties.
3. **Switch subscriptions when the followed object changes.** Unsubscribe from the old object
   before you subscribe to the new one:

   ```csharp
   private void SetMainForecast(WeatherForecast? forecast)
   {
       mainForecast?.OnChange -= OnForecastChanged;
       mainForecast = forecast;
       mainForecast?.OnChange += OnForecastChanged;
       UpdateLabel();
   }
   ```

4. **Follow collections with `Added`/`Removed`/`Cleared`**, and add the existing items first
   (`foreach (var item in Service.Xxx.Items) Add(item);`). Keep a dictionary from item to its
   UI and handlers. Remove both together.
5. **Do not write back in a `Changed` handler** to the property that sent it. This makes a
   loop. If a GTK control both shows and changes a value (a `Switch` that follows `Powered`),
   use a guard flag (`isUpdatingSwitch` in `BluetoothPopover`).
6. **Settings that affect formatting** (`TimeFormat`, `DateFormat`, `LanguageCode`) are
   reactive too. Subscribe to them like to any other property. The culture comes from
   `languageCode`. Use `CultureInfo.CurrentCulture` for text for the user, and
   `CultureInfo.InvariantCulture` for anything machine-readable.

---

## 4. Threads: never block, never touch GTK off the main thread

The app runs with `application.RunWithSynchronizationContext(null)`. This means:

- **After `await` you are back on the main thread**, if the `await` started on the main thread
  and you did not use `ConfigureAwait(false)`. Do not use `ConfigureAwait(false)` in services
  or widgets.
- **GTK and the reactive values must be changed only on the main thread.**

What to use:

| Work | How | Example |
|---|---|---|
| HTTP, D-Bus calls that take time | `async` + `await` on the main thread | `WeatherService.UpdateWeatherAsync`, `BluezBus.CallAsync` |
| A loop that waits forever (socket, stream) | `Task.Run(...)`, then `GLib.Functions.IdleAdd` to go back to the main thread | `HyprlandService.FollowEventsAsync` + `RunOnMainThread` |
| Native callbacks (PipeWire) | Copy the data, then `IdleAdd` to the main thread | `PipeWireConnection.OnGlobal` |
| Periodic work | `GLib.Functions.TimeoutAdd` (main thread), keep the id, `SourceRemove` it | `CalendarWidget`, `WeatherService` |
| Short local D-Bus calls at startup | `CallSync` is acceptable | `BluetoothService.Reload` (GetManagedObjects) |

Rules:

- **`CallSync`, `.Result`, `.Wait()` and `Thread.Sleep` block the bar.** Use them only for
  calls that are fast and local. `Connect` and `Pair` take seconds, so they must be async.
- **Fire-and-forget**: write `_ = DoAsync();` and put a `try/catch` **inside** `DoAsync`.
  Nobody observes the task, so an exception that you do not catch is lost. Do not use
  `async void` (except in GTK signal handlers, and then wrap the body in `try/catch`).
- **Cancel the work that is no longer needed.** Use one `CancellationTokenSource` for each kind
  of work. Cancel the old one when you start new work (`RestartUpdate`) and in `Dispose`.
  Catch `OperationCanceledException` **only** `when (token.IsCancellationRequested)`. Without a
  cancelled token, it is a timeout (for example the `HttpClient` timeout) and you must log it.
- **A callback can come after `Dispose`.** An `IdleAdd` callback that was queued before
  `Dispose` still runs. Check the token in it, as `HyprlandService.RunOnMainThread` does.
- **Native callbacks must never throw.** An exception in an
  `[UnmanagedCallersOnly]` method kills the process. Wrap the whole body in `try/catch`, and
  copy native data before the callback returns.
- **Respect remote services.** Nominatim allows 1 request per second (`GEOCODING_DELAY_MS`),
  so keep the results in a cache. Open-Meteo gets all the locations in one request.

---

## 5. Lifetime and memory leaks

In C#, an event keeps its **subscriber** alive. In this app the publishers (services,
`UserSettings`) live for the whole process. The subscribers (widgets) do not. Every
subscription from a widget to a service or the settings must be removed.

There are two accepted ways to do it:

**A) `IWithDisposableService` + `MainBar.RemoveServices()`**: for bar buttons, popovers and
their elements. The bar calls `RemoveService()` when it closes. Each level calls the same
method on its children:

```
MainBar.RemoveServices()
 └─ WeatherBarButton.RemoveService()
     └─ WeatherPopover.RemoveService()
         └─ WeatherLocationPage.Dispose()   (for each tab)
```

**B) `OnMap` / `OnUnmap`**: for simple widgets that follow only settings or one value
(`CalendarWidget`, `WorkspaceWidget`). They subscribe when they are shown and unsubscribe when
they are hidden. They need no entry in `RemoveServices`. Call `Update()` in `OnMap`, because
the value can change while the widget is not shown.

Use (A) when the widget must stay up to date while it is hidden (the popover state, the lists).
Use (B) when the widget is only a view on the bar.

The leak checklist:

- [ ] Each `+=` to a service, a model owned by a service, or `UserSettings` has a `-=`.
- [ ] Each `GLib.Functions.TimeoutAdd`/`IdleAdd` that repeats has its id stored and is removed
      (`SourceRemove`) in `Dispose`/`OnDestroy`.
- [ ] Each `CancellationTokenSource` is cancelled in `Dispose`.
- [ ] `HttpClient`, `FileMonitor` and D-Bus proxies of the service are disposed in `Dispose`.
- [ ] Each `GLib.Variant` that you get from D-Bus is in a `using` (`using var x = ...GetChildValue(0);`).
- [ ] List rows unsubscribe in `Unbind`. Lists unsubscribe from all their items in `Dispose`.
- [ ] When you remove an item from a dictionary of UI elements, you also remove its handlers
      and dispose the element (`WeatherPopover.RemoveLocation`).
- [ ] `RemoveService()` is safe to call twice. `RemoveServices` runs on close and again on quit.
- [ ] The new widget is in `MainBar.RemoveServices()` (or uses pattern B).

---

## 6. Network and external data

- **One `HttpClient` per service**, created in the constructor with `BaseAddress`, disposed in
  `Dispose`. Do not create one per request.
- Some APIs need a `User-Agent` (Nominatim):
  `client.DefaultRequestHeaders.UserAgent.ParseAdd("OvraelShell")`.
- **Use `CultureInfo.InvariantCulture` for numbers in URLs and files.** With the Polish culture
  `50.25` becomes `50,25`, which breaks a comma-separated list of coordinates. Use
  `string.Create(CultureInfo.InvariantCulture, $"...")`.
- Parse the response into small DTO classes (`Models/Weather/OpenMeteo/`). Convert them to your
  own model (`WeatherData`). Widgets never see the API format.
- Assume that any field can be missing. Use nullable DTO properties and keep the old value
  when a section is missing (`WeatherForecast.Update`).
- Check that the response matches the request (count, order) before you apply it. If it does
  not match, log it and ignore it.

---

## 7. UI conventions

- Every clickable widget: `SetCursor(Cursors.Pointer)`. A disabled action:
  `Cursors.NotAllowed`.
- Icons are Nerd Font glyphs in `InterfaceElements/Icons.cs`. Put new icons in a nested class
  (`Icons.Xxx`). JSON is written with `MinimalJsonEncoder`, so glyphs stay readable in the
  settings file.
- Show an "empty" state in the place of the content ("No new devices", "No weather data").
  Do not show an empty list.
- A label on the bar must stay short. Limit names that come from outside (see
  `WeatherService.LimitName`, 30 characters with `…`).
- Text for the user: English, short. Log messages: `"Xxx: what happened: {ex.Message}"`.

---

## 8. Testing your widget

You usually run the shell with `debug.sh` (`dotnet watch run`). Keep that instance and test
in a separate one:

```fish
set S /tmp/ovrael-test
mkdir -p $S/Data $S/cfg
cp OvraelShell/Data/settings.json $S/Data/
ln -sfn (pwd)/OvraelShell/Styles $S/Styles        # styles load from the current directory

dotnet build OvraelShell -o $S/bin
cd $S
GTK_A11Y=none LD_PRELOAD=/usr/lib/libgtk4-layer-shell.so.0 XDG_CONFIG_HOME=$S/cfg \
    dbus-run-session -- dotnet $S/bin/OvraelShell.dll
```

- `dbus-run-session` gives a separate session bus. Without it, the second launch only activates
  the running instance and makes it create another bar. The system bus (BlueZ,
  NetworkManager) is shared, so do not test destructive actions on real devices.
- `GTK_A11Y=none`: without it, the test instance can break the accessibility bus for all your
  GTK apps.
- Run it from the test directory. `Data/settings.json` in the current directory has priority over
  `$XDG_CONFIG_HOME`.
- `dotnet watch` cannot apply some edits ("rude edits", for example ENC0033 after you remove a
  field). It asks for a restart. If you answer with Ctrl+C, the watch stops.
- To see a popover without clicking, put a temporary
  `GLib.Functions.TimeoutAdd(0, 5000, () => { popover.Popup(); return false; });` in a **copy**
  of the project, and take a screenshot with `grim`. Do not put debug code in the real sources
  while `dotnet watch` runs.

Before you commit, test:

- [ ] The daemon/API is not available (stop the service, disconnect the network).
- [ ] The settings file: a missing section, an incorrect value, a save while the app runs.
- [ ] The popover opens and closes many times (no growing logs, no duplicate items).
- [ ] The theme change (if the widget has themed styles).
- [ ] The app quits without exceptions in the log.

---

## 9. Final checklist

**Model and service**
- [ ] State in `ReactiveProperty`/`ReactiveCollection`. Comparers for lists, records for values.
- [ ] Stable keys (path, coordinates), not names.
- [ ] Reactive values change only on the main thread.
- [ ] No blocking calls on the main thread. Long work is `async` or on `Task.Run` + `IdleAdd`.
- [ ] Cancellation for each kind of work. The old work is cancelled when new work starts.
- [ ] `Dispose` undoes everything. The service is created with `??=` and disposed in `OnShutdown`.

**Widgets**
- [ ] `Initialize` builds the UI. `New` adds the service and builds what needs it.
- [ ] `[MemberNotNull]` on the methods that set fields.
- [ ] Named handlers for long-lived publishers. Each `+=` has a `-=`.
- [ ] One `Update()` that renders the whole state. It is called after each change and at the start.
- [ ] `RemoveService()` cascades to the popover and the elements, and is safe to call twice.
- [ ] Registered in `MainBar.RemoveServices()` (or uses `OnMap`/`OnUnmap`).
- [ ] Lists: `ListView` + factory. Rows unsubscribe in `Unbind`.
- [ ] Popover: no size change while it is open. The keyboard is requested and released if
      there is an entry. Expensive work happens only while it is shown.
- [ ] Pointer cursor on everything clickable.

**Settings and styles**
- [ ] Section with defaults. Validation logs and falls back, never throws.
- [ ] Own CSS classes with a prefix. Colors only in `Styles/Themed`, through `@define-color` names.

**Testing**
- [ ] Tested in an isolated instance, with the service unavailable and with incorrect settings.
