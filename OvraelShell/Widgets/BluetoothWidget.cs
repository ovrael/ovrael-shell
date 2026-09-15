using System.Diagnostics.CodeAnalysis;
using Gtk;

namespace OvraelShell.Widgets;

[GObject.Subclass<Gtk.Box>]
public sealed partial class BluetoothWidget
{
    private Label label;

    public static BluetoothWidget New()
    {
        return NewWithProperties([]);
    }

    [MemberNotNull(nameof(label))]
    partial void Initialize()
    {
        SetOrientation(Orientation.Horizontal);
        label = Label.New("󰂯");
        Append(label);
    }
}
