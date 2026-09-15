using Gio;
using Gtk;
using OvraelShell;
using ZwlrLayerShell;

const int X_MARGIN = 0;
const int Y_MARGIN = 0;

const int BAR_WIDTH = 3440;
const int BAR_HEIGTH = 40;

var application = Gtk.Application.New("io.ovrael.shell", Gio.ApplicationFlags.FlagsNone);
CssLoader cssLoader = new(CssTheme.Dark);

application.OnActivate += (sender, e) =>
{
    if (!LayerShell.IsSupported())
    {
        Console.WriteLine("Compositor doesn't support zwlr-layer-shell.");
        return;
    }

    cssLoader.LoadCss();

    CreateBar(sender);
};

return application.RunWithSynchronizationContext(null);

void CreateBar(Gio.Application sender)
{
    var window = MainBar.New(BAR_WIDTH, BAR_HEIGTH);
    // Set the "Application" property of the window to the current application instance.
    // This links the window to the application, allowing them to work together.
    window.Application = (Gtk.Application)sender;
    SetLayerShell(window);

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

    // Set exlusive so other windows won't hide beneath it
    LayerShell.SetAutoExclusiveZone(window);

    // Margins
    LayerShell.SetMargin(window, Edge.Top, Y_MARGIN);
    LayerShell.SetMargin(window, Edge.Bottom, Y_MARGIN);
    LayerShell.SetMargin(window, Edge.Left, X_MARGIN);
    LayerShell.SetMargin(window, Edge.Right, X_MARGIN);
}
