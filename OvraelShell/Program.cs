using Gtk;
using OvraelShell.Enums.Settings;
using OvraelShell.Services;
using OvraelShell.Utils.Settings;
using OvraelShell.Utils.Theme;
using OvraelShell.Widgets;
using ZwlrLayerShell;

const int X_MARGIN = 0;
const int Y_MARGIN = 0;

const int BAR_WIDTH = 3440;
const int BAR_HEIGTH = 40;

var application = Gtk.Application.New("io.ovrael.shell", Gio.ApplicationFlags.FlagsNone);

// One for the whole application - every bar shows the same state
ShellServices? services = null;

// Open bars - GetWindows gives a raw GLib list, so they are tracked here
var bars = new List<MainBar>();

application.OnActivate += (sender, e) =>
{
    if (!LayerShell.IsSupported())
    {
        Console.WriteLine("Compositor doesn't support zwlr-layer-shell.");
        return;
    }

    // Before the bars - their widgets read the settings when they are created
    UserSettings.Load();

    ThemeManager.Load(ThemeScheme.Dark);

    // Activate runs again when the application is launched a second time
    services ??= new ShellServices();

    CreateBar(sender, services);
};

application.OnShutdown += (_, _) =>
{
    // Quitting destroys the bars without close-request - detach them first,
    // so disposing the service does not update their widgets
    foreach (var bar in bars)
        bar.RemoveServices();

    bars.Clear();

    services?.Dispose();

    UserSettings.Unload();
};

return application.RunWithSynchronizationContext(null);

void CreateBar(Gio.Application sender, ShellServices services)
{
    var window = MainBar.New(services, BAR_WIDTH, BAR_HEIGTH);
    // Set the "Application" property of the window to the current application instance.
    // This links the window to the application, allowing them to work together.
    window.Application = (Gtk.Application)sender;
    SetLayerShell(window);

    // A closed bar has already detached itself (MainBar.OnCloseRequest)
    bars.Add(window);
    window.OnCloseRequest += (_, _) =>
    {
        bars.Remove(window);
        return false;
    };

    window.Present();
}

void SetLayerShell(Window window)
{
    // Init layer
    LayerShell.InitForWindow(window);

    // Set layer to top - almost most important (right before overlay)
    LayerShell.SetLayer(window, Layer.Top);

    // Name - helps with hyprland or debugging
    LayerShell.SetNamespace(window, "hyprbar");

    // Anchors
    LayerShell.SetAnchor(window, Edge.Left, true);
    LayerShell.SetAnchor(window, Edge.Right, true);
    LayerShell.SetAnchor(window, Edge.Bottom, true);
    LayerShell.SetAnchor(window, Edge.Top, false);

    // No keyboard by default - popovers that need typing (Wi-Fi password) request it
    // only while they are open, see NetworkPopover.SetBarKeyboardMode
    LayerShell.SetKeyboardMode(window, KeyboardMode.None);

    // Set exlusive so other windows won't hide beneath it
    LayerShell.SetAutoExclusiveZone(window);

    // Margins
    LayerShell.SetMargin(window, Edge.Top, Y_MARGIN);
    LayerShell.SetMargin(window, Edge.Bottom, Y_MARGIN);
    LayerShell.SetMargin(window, Edge.Left, X_MARGIN);
    LayerShell.SetMargin(window, Edge.Right, X_MARGIN);
}
