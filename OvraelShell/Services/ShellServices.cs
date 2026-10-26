namespace OvraelShell.Services;

/// <summary>
/// The services of the application - one of each, shared by all bars.
/// Created on the first Activate, disposed when the application quits.
/// </summary>
public sealed class ShellServices : IDisposable
{
    // Created in this order, disposed in the same order
    public NetworkService Network { get; } = new();
    public AudioService Audio { get; } = new();
    public BluetoothService Bluetooth { get; } = new();
    public HyprlandService Hyprland { get; } = new();
    public WeatherService Weather { get; } = new();
    public SystemStatusService SystemStatus { get; } = new();
    public ObsidianService Obsidian { get; } = new();
    public ClipboardService Clipboard { get; } = new();
    public MediaService Media { get; } = new();
    public NotificationsService Notifications { get; } = new();
    public TrayService Tray { get; } = new();
    public KdeConnectService KdeConnect { get; } = new();
    public TrashService Trash { get; } = new();

    /// <summary>Detach the bars first, so disposing a service does not update their widgets.</summary>
    public void Dispose()
    {
        Network.Dispose();
        Audio.Dispose();
        Bluetooth.Dispose();
        Hyprland.Dispose();
        Weather.Dispose();
        SystemStatus.Dispose();
        Obsidian.Dispose();
        Clipboard.Dispose();
        Media.Dispose();
        Notifications.Dispose();
        Tray.Dispose();
        KdeConnect.Dispose();
        Trash.Dispose();
    }
}
