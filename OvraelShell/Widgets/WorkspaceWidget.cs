using System.Diagnostics.CodeAnalysis;
using Gtk;

namespace OvraelShell.Widgets;

// Define a class named WorkspaceWidget that extends Gtk.Window.
// Gtk.Window represents a top-level window in a GTK application.
// Subclassing requires a source generator to integrate deeply with
// the GObject type system.
[GObject.Subclass<Gtk.Box>]
public partial class WorkspaceWidget
{
    private Gtk.Label label;

    public static WorkspaceWidget New()
    {
        return NewWithProperties([]);
    }

    [MemberNotNull(nameof(label))]
    partial void Initialize()
    {
        SetOrientation(Orientation.Horizontal);
        label = Gtk.Label.New("1  2  3  4");

        Append(label);
    }
}
