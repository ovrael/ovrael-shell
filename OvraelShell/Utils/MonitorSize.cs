namespace OvraelShell.Utils;

public static class MonitorSize
{
    /// <summary>Height of the monitor <paramref name="widget"/> is shown on, or 0 when it is not shown.</summary>
    public static int HeightOf(Gtk.Widget widget) => GeometryOf(widget)?.Height ?? 0;

    /// <summary>Width of the monitor <paramref name="widget"/> is shown on, or 0 when it is not shown.</summary>
    public static int WidthOf(Gtk.Widget widget) => GeometryOf(widget)?.Width ?? 0;

    private static Gdk.Rectangle? GeometryOf(Gtk.Widget widget)
    {
        var display = Gdk.Display.GetDefault();
        var surface = widget.GetNative()?.GetSurface();

        if (display is null || surface is null)
            return null;

        var monitor = display.GetMonitorAtSurface(surface);

        if (monitor is null)
            return null;

        monitor.GetGeometry(out var geometry);

        return geometry;
    }
}
