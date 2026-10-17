using Gtk;
using ZwlrLayerShell;

namespace OvraelShell.Utils;

/// <summary>
/// The bar takes no keyboard (KeyboardMode.None). A popover that needs typing
/// (Wi-Fi password, device name) requests it while it is open.
/// </summary>
public static class BarKeyboard
{
    /// <summary>Call when the popover opens.</summary>
    public static void Request(Popover popover) => SetMode(popover, KeyboardMode.OnDemand);

    /// <summary>
    /// Call when the popover closes. With OnDemand the compositor keeps focus on the bar
    /// until another window is clicked, so typing elsewhere would not work after it closes.
    /// </summary>
    public static void Release(Popover popover)
    {
        GetBarWindow(popover)?.SetFocus(null);
        SetMode(popover, KeyboardMode.None);
    }

    private static Gtk.Window? GetBarWindow(Popover popover) =>
        popover.GetParent()?.GetRoot() as Gtk.Window;

    private static void SetMode(Popover popover, KeyboardMode mode)
    {
        if (GetBarWindow(popover) is { } window && LayerShell.IsLayerWindow(window))
            LayerShell.SetKeyboardMode(window, mode);
    }
}
