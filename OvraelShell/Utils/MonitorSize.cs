namespace OvraelShell.Utils;

public static class MonitorSize
{
    /// <summary>Height of the monitor <paramref name="widget"/> is shown on, or 0 when it is not shown.</summary>
    public static int HeightOf(Gtk.Widget widget)
    {
        var display = Gdk.Display.GetDefault();
        var surface = widget.GetNative()?.GetSurface();

        if (display is null || surface is null)
            return 0;

        var monitor = display.GetMonitorAtSurface(surface);

        if (monitor is null)
            return 0;

        monitor.GetGeometry(out var geometry);

        return geometry.Height;
    }
}
