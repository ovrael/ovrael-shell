using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;

namespace OvraelShell.Widgets.Network.Elements;

/// <summary>"Connect" button of a network row - reads "Hide" while the password field is open.</summary>
[GObject.Subclass<Gtk.Button>]
public sealed partial class ConnectButton
{
    // A Stack is as wide as its widest child,
    // so the button keeps the same width when the label switches
    private Stack labels;

    public static new ConnectButton New()
    {
        return NewWithProperties([]);
    }

    [MemberNotNull(nameof(labels))]
    partial void Initialize()
    {
        labels = Stack.New();
        labels.AddNamed(Gtk.Label.New("Connect"), "connect");
        labels.AddNamed(Gtk.Label.New("Hide"), "hide");

        SetChild(labels);

        // GTK adds text-button (wider padding) only for buttons made with a label -
        // without it this button is narrower than the "Connect" next to the password
        AddCssClass("text-button");
        SetCursor(Cursors.Pointer);
    }

    public void ShowHideLabel(bool hide) => labels.SetVisibleChildName(hide ? "hide" : "connect");
}
