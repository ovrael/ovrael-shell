using System.Diagnostics.CodeAnalysis;
using GLib;
using Gtk;
using OvraelShell.Models.Bluetooth;
using OvraelShell.Services;
using OvraelShell.Utils;
using OvraelShell.Widgets.Bluetooth.Elements;
using Task = System.Threading.Tasks.Task;

namespace OvraelShell.Widgets.Bluetooth;

[GObject.Subclass<Gtk.Popover>]
public sealed partial class BluetoothPopover : IWithDisposableService<BluetoothService>
{
    // Share of the monitor height the whole popover takes - see UpdatePagesHeight
    private const double HeightRatio = 0.45;

    private const int MinPagesHeight = 150;

    // Before the bar is on a monitor
    private const int FallbackPagesHeight = 400;

    // Content margins (2 x 12) plus .popover contents padding and border (2 x 10 + 2 x 1)
    private const int PopoverChrome = 24 + 22;

    public BluetoothService Service { get; private set; }

    // Created in Initialize, which runs before AddService
    private Box content;
    private Box header;
    private Switch powerSwitch;
    private Label statusLabel;
    private StackSwitcher tabs;

    // Homogeneous (the default), so switching tabs does not resize the popover
    private Stack pages;

    // Need the service, so they are created in AddService
    private readonly List<DevicesPage> devicePages = [];

    private BluetoothAdapter? adapter;

    // The last failed power change - cleared by the next one
    private string? powerError;

    // SetActive emits state-set too - only the user's changes go to BlueZ
    private bool isUpdatingSwitch;

    public static BluetoothPopover New(BluetoothService bluetoothService, Button parent)
    {
        var widget = NewWithProperties([]);
        widget.AddService(bluetoothService);
        widget.SetParent(parent);
        return widget;
    }

    [MemberNotNull(
        nameof(content),
        nameof(header),
        nameof(powerSwitch),
        nameof(statusLabel),
        nameof(tabs),
        nameof(pages)
    )]
    partial void Initialize()
    {
        CreateContent();

        SetPosition(PositionType.Top);
        SetAutohide(true);
        SetChild(content);
        AddCssClass("popover");

        OnShow += (_, _) => OnShown();
        OnHide += (_, _) => OnHidden();
    }

    [MemberNotNull(nameof(Service))]
    public void AddService(BluetoothService bluetoothService)
    {
        Service = bluetoothService;

        // New on the left, known on the right
        AddDevicesPage(bluetoothService, showsKnown: false, "new", "New", "No new devices");
        AddDevicesPage(bluetoothService, showsKnown: true, "known", "Known", "No known devices");

        bluetoothService.Adapter.Changed += SetAdapter;
        SetAdapter(bluetoothService.Adapter.Value);
    }

    public void RemoveService()
    {
        Service.Adapter.Changed -= SetAdapter;
        SetAdapter(null);

        foreach (var page in devicePages)
        {
            page.List.ItemsChanged -= page.OnItemsChanged;
            page.List.Dispose();
        }

        devicePages.Clear();
    }

    [MemberNotNull(
        nameof(content),
        nameof(header),
        nameof(powerSwitch),
        nameof(statusLabel),
        nameof(tabs),
        nameof(pages)
    )]
    private void CreateContent()
    {
        content = Box.New(Orientation.Vertical, 8);
        content.SetMarginTop(12);
        content.SetMarginBottom(12);
        content.SetMarginStart(12);
        content.SetMarginEnd(12);

        var title = Label.New("Bluetooth");
        title.SetXalign(0);
        title.Hexpand = true;

        powerSwitch = Switch.New();
        powerSwitch.SetValign(Align.Center);
        powerSwitch.SetCursor(InterfaceElements.Cursors.Pointer);
        powerSwitch.OnStateSet += (sender, args) =>
        {
            if (!isUpdatingSwitch)
                _ = SetPoweredAsync(args.State);

            // The state follows Powered from BlueZ, see UpdateSwitch
            return true;
        };

        header = Box.New(Orientation.Horizontal, 10);
        header.Append(title);
        header.Append(powerSwitch);
        content.Append(header);

        statusLabel = Label.New(string.Empty);
        statusLabel.SetWrap(true);
        statusLabel.SetMaxWidthChars(40);
        statusLabel.AddCssClass("bluetooth-status");
        content.Append(statusLabel);

        pages = Stack.New();

        tabs = StackSwitcher.New();
        tabs.SetStack(pages);
        tabs.SetHalign(Align.Center);
        tabs.AddCssClass("bluetooth-tabs");

        content.Append(tabs);
        content.Append(pages);
    }

    #region Device pages

    /// <summary>A tab: the list in a scroll, and a line in its place when it is empty.</summary>
    private sealed record DevicesPage(
        BluetoothDeviceList List,
        ScrolledWindow Scroll,
        Label EmptyLabel,
        Action OnItemsChanged
    );

    private void AddDevicesPage(
        BluetoothService bluetoothService,
        bool showsKnown,
        string name,
        string title,
        string emptyText
    )
    {
        var list = new BluetoothDeviceList(bluetoothService, showsKnown);

        // Fills the page - its height is set in UpdatePagesHeight, the list scrolls in it
        var scroll = ScrolledWindow.New();
        scroll.SetPolicy(PolicyType.Never, PolicyType.Automatic);
        scroll.Vexpand = true;
        scroll.SetChild(list.View);

        var emptyLabel = Label.New(emptyText);
        emptyLabel.AddCssClass("bluetooth-empty");
        emptyLabel.SetValign(Align.Start);
        emptyLabel.Vexpand = true;

        var box = Box.New(Orientation.Vertical, 0);
        box.Append(scroll);
        box.Append(emptyLabel);

        DevicesPage? page = null;
        page = new DevicesPage(list, scroll, emptyLabel, () => UpdateEmpty(page!));

        list.ItemsChanged += page.OnItemsChanged;
        devicePages.Add(page);
        UpdateEmpty(page);

        pages.AddTitled(box, name, title);
    }

    private static void UpdateEmpty(DevicesPage page)
    {
        page.Scroll.Visible = !page.List.IsEmpty;
        page.EmptyLabel.Visible = page.List.IsEmpty;
    }

    #endregion

    #region Adapter

    private void SetAdapter(BluetoothAdapter? newAdapter)
    {
        if (adapter is not null)
        {
            adapter.Powered.Changed -= OnAdapterStateChanged;
            adapter.Discovering.Changed -= OnAdapterStateChanged;
            adapter.PowerState.Changed -= OnAdapterStateChanged;
        }

        adapter = newAdapter;
        powerError = null;

        if (adapter is not null)
        {
            adapter.Powered.Changed += OnAdapterStateChanged;
            adapter.Discovering.Changed += OnAdapterStateChanged;
            adapter.PowerState.Changed += OnAdapterStateChanged;
        }

        UpdateState();
    }

    private void OnAdapterStateChanged<T>(T _) => UpdateState();

    private async Task SetPoweredAsync(bool powered)
    {
        if (adapter is null)
            return;

        powerError = null;
        try
        {
            await adapter.SetPoweredAsync(powered);
        }
        catch (GException exception)
        {
            Console.WriteLine($"Bluetooth: cannot change the power: {exception.Message}");
            powerError = BluezBus.ErrorMessage(exception);
        }

        // Back to the real state if it failed
        UpdateState();
    }

    private void UpdateState()
    {
        UpdateSwitch();

        var status = GetStatus();
        // Always shown, so the popover does not resize when "Searching..." comes and goes
        statusLabel.SetLabel(status ?? string.Empty);

        // Tabs only make sense while on
        var isPowered = adapter?.Powered.Value == true;
        tabs.Visible = isPowered;
        pages.Visible = isPowered;
    }

    private void UpdateSwitch()
    {
        var powered = adapter?.Powered.Value == true;

        isUpdatingSwitch = true;
        powerSwitch.SetActive(powered);
        powerSwitch.SetState(powered);
        isUpdatingSwitch = false;

        powerSwitch.Sensitive = adapter is not null;
    }

    private string? GetStatus()
    {
        if (adapter is null)
            return "No Bluetooth adapter";

        if (powerError is not null)
            return powerError;

        if (adapter.PowerState.Value == "off-blocked")
            return "Bluetooth is blocked (rfkill)";

        if (!adapter.Powered.Value)
            return "Bluetooth is off";

        return adapter.Discovering.Value ? "Searching for devices..." : null;
    }

    #endregion

    private void OnShown()
    {
        // Keyboard only while open - needed to type a device name
        BarKeyboard.Request(this);

        UpdatePagesHeight();

        Service.SetDiscovery(true);
    }

    private void OnHidden()
    {
        BarKeyboard.Release(this);
        Service.SetDiscovery(false);
    }

    /// <summary>
    /// Gives the pages a fixed height, so the whole popover takes about
    /// <see cref="HeightRatio"/> of the monitor the bar is on. Fixed, because every size change
    /// makes GTK present the popup again (xdg_popup reposition), which flickers on Hyprland -
    /// and the discovery adds and removes devices all the time. The pages share the height,
    /// so switching tabs does not resize either.
    /// Done on every show - the bar can move between monitors.
    /// </summary>
    private void UpdatePagesHeight()
    {
        var monitorHeight = GetParent() is { } parent ? MonitorSize.HeightOf(parent) : 0;

        if (monitorHeight <= 0)
        {
            pages.SetSizeRequest(-1, FallbackPagesHeight);
            return;
        }

        // Everything above the pages: header, status line, tabs and the gaps between them
        header.Measure(Orientation.Vertical, -1, out _, out var headerHeight, out _, out _);
        statusLabel.Measure(Orientation.Vertical, -1, out _, out var statusHeight, out _, out _);
        tabs.Measure(Orientation.Vertical, -1, out _, out var tabsHeight, out _, out _);

        var reserved =
            headerHeight + statusHeight + tabsHeight + 3 * content.GetSpacing() + PopoverChrome;
        var pagesHeight = Math.Max(MinPagesHeight, (int)(monitorHeight * HeightRatio) - reserved);

        pages.SetSizeRequest(-1, pagesHeight);
    }
}
