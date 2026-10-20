using Gtk;

namespace OvraelShell.Utils;

public static class HorizontalScroll
{
    // Pixels for one step of a mouse wheel
    private const double WheelStep = 60;

    /// <summary>
    /// The mouse wheel (vertical) scrolls <paramref name="scroll"/> horizontally - for a
    /// ScrolledWindow that scrolls only horizontally. A touchpad sends horizontal scroll,
    /// the ScrolledWindow uses it itself.
    /// </summary>
    public static void AddMouseWheel(ScrolledWindow scroll)
    {
        var wheel = EventControllerScroll.New(EventControllerScrollFlags.Vertical);

        // Capture - before the scroll controller of the ScrolledWindow
        wheel.SetPropagationPhase(PropagationPhase.Capture);
        wheel.OnScroll += (controller, args) => Scroll(scroll, controller, args.Dy);
        scroll.AddController(wheel);
    }

    private static bool Scroll(ScrolledWindow scroll, EventControllerScroll controller, double dy)
    {
        var adjustment = scroll.GetHadjustment();

        // All the content is visible
        if (adjustment.GetUpper() - adjustment.GetLower() <= adjustment.GetPageSize())
            return false;

        var step = controller.GetUnit() == Gdk.ScrollUnit.Wheel ? dy * WheelStep : dy;
        adjustment.SetValue(adjustment.GetValue() + step);
        return true;
    }
}
