namespace OvraelShell.Widgets;

[GObject.Subclass<Gtk.Label>]
public sealed partial class ClockWidget
{
    private uint timeoutId;

    public static ClockWidget New()
    {
        return NewWithProperties([]);
    }

    partial void Initialize()
    {
        SetText(GetCurrentTime());

        timeoutId = GLib.Functions.TimeoutAdd(0, 1000, Update);

        OnDestroy += (_, _) => GLib.Functions.SourceRemove(timeoutId);
    }

    private bool Update()
    {
        SetText(GetCurrentTime());
        return true;
    }

    private static string GetCurrentTime()
    {
        return DateTime.Now.ToString("HH:mm:ss");
    }
}
