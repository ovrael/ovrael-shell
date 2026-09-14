using Gtk;

namespace OvraelShell.Widgets;

// Define a class named WorkspaceWidget that extends Gtk.Window.
// Gtk.Window represents a top-level window in a GTK application.
// Subclassing requires a source generator to integrate deeply with
// the GObject type system.
[GObject.Subclass<Gtk.Box>]
public partial class WorkspaceWidget
{
    private Gtk.Label _label;

    public static WorkspaceWidget New()
    {
        return NewWithProperties([]);
    }

    // Initializer for the WorkspaceWidget class.
    // This method sets up the window and its contents.
    // Gtk.Application is passed in so we can access the application in this app
    // we will use the reference to the Gtk.Application to quit from a button.
    partial void Initialize()
    {
        SetOrientation(Orientation.Horizontal);
        _label = Gtk.Label.New("1  2  3  4");

        Append(_label);
    }
}
