using System.Diagnostics.CodeAnalysis;
using Gtk;

namespace OvraelShell.Widgets;

[GObject.Subclass<Gtk.Box>]
public sealed partial class CalendarWidget
{
    private uint timeoutId;
    private Label label;

    public static CalendarWidget New()
    {
        return NewWithProperties([]);
    }

    [MemberNotNull(nameof(label))]
    partial void Initialize()
    {
        SetOrientation(Orientation.Horizontal);
        label = Label.New(GetCurrentTime());
        timeoutId = GLib.Functions.TimeoutAdd(0, 1000, Update);
        Append(label);

        OnDestroy += (_, _) => GLib.Functions.SourceRemove(timeoutId);
    }

    private bool Update()
    {
        label.SetText(GetCurrentTime());
        return true;
    }

    private static string GetCurrentTime()
    {
        return DateTime.Now.ToString("HH:mm:ss");
    }
}
