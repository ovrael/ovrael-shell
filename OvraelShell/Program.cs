using Gio;
// var application = Gtk.Application.New("io.ovrael.shell", Gio.ApplicationFlags.FlagsNone);

// application.OnActivate += (sender, args) =>
// {
//     // Create a new instance of the main application window.
//     // The application variable storing the Gtk.Application instance is passed to
//     // the new instance of the BoxLayout window so that the window can access the
//     // Gtk.Application instance, this is useful for closing the application from
//     // a button.
//     var window = ShellWindow.New();

//     // Set the "Application" property of the window to the current application instance.
//     // This links the window to the application, allowing them to work together.
//     window.Application = (Gtk.Application)sender;

//     // Show the window on the screen.
//     // This makes the window visible to the user.
//     window.Show();
// };

// return application.RunWithSynchronizationContext(null);

using Gtk;
using OvraelShell;
using ZwlrLayerShell;

var application = Gtk.Application.New("io.ovrael.shell", Gio.ApplicationFlags.FlagsNone);

application.OnActivate += (sender, e) =>
{
    if (!LayerShell.IsSupported())
    {
        Console.Error.WriteLine("Kompozytor nie wspiera zwlr-layer-shell.");
        return;
    }

    var window = ShellWindow.New();

    // 1. Nadaj oknu rolę "layer surface" — MUSI być wywołane
    //    przed window.Present()
    LayerShell.InitForWindow(window);

    // 2. Warstwa: Top rysuje się nad zwykłymi oknami, Bottom - pod nimi
    LayerShell.SetLayer(window, Layer.Top);

    // 3. Nazwa surface'a (przydatna np. w regułach Hyprlanda / debugowaniu)
    LayerShell.SetNamespace(window, "hyprbar");

    // 4. Zakotwiczenie do lewej, prawej i dolnej krawędzi = pasek na dole,
    //    rozciągnięty na całą szerokość
    LayerShell.SetAnchor(window, Edge.Left, true);
    LayerShell.SetAnchor(window, Edge.Right, true);
    LayerShell.SetAnchor(window, Edge.Bottom, true);
    // (Edge.Top zostaw "false" - wtedy okno przyklei się do dołu,
    // a jego wysokość to wysokość zawartości)

    // 5. Zarezerwuj miejsce, żeby inne okna nie chowały się pod paskiem
    LayerShell.SetAutoExclusiveZone(window);

    // (opcjonalnie) margines od krawędzi ekranu
    LayerShell.SetMargin(window, Edge.Bottom, 4);
    LayerShell.SetMargin(window, Edge.Left, 8);
    LayerShell.SetMargin(window, Edge.Right, 8);

    // Zawartość paska
    window.Present();
};

return application.RunWithSynchronizationContext(null);
