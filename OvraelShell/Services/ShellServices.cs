namespace OvraelShell.Services;

/// <summary>
/// The services of the application - one of each, shared by all bars.
/// Created on the first Activate, disposed when the application quits.
/// </summary>
public sealed class ShellServices : IDisposable
{
    private NetworkService? network;
    private AudioService? audio;
    private BluetoothService? bluetooth;
    private HyprlandService? hyprland;
    private WeatherService? weather;
    private SystemStatusService? systemStatus;
    private ObsidianService? obsidian;
    private ClipboardService? clipboard;
    private MediaService? media;
    private NotificationsService? notifications;
    private TrayService? tray;
    private KdeConnectService? kdeConnect;
    private TrashService? trash;
    private CalendarService? calendar;

    // Created on the first use - the service of a widget that is not loaded is not started
    public NetworkService Network => network ??= new();
    public AudioService Audio => audio ??= new();
    public BluetoothService Bluetooth => bluetooth ??= new();
    public HyprlandService Hyprland => hyprland ??= new();
    public WeatherService Weather => weather ??= new();
    public SystemStatusService SystemStatus => systemStatus ??= new();
    public ObsidianService Obsidian => obsidian ??= new();
    public ClipboardService Clipboard => clipboard ??= new();
    public MediaService Media => media ??= new();
    public NotificationsService Notifications => notifications ??= new();
    public TrayService Tray => tray ??= new();
    public KdeConnectService KdeConnect => kdeConnect ??= new();
    public TrashService Trash => trash ??= new();
    public CalendarService Calendar => calendar ??= new();

    /// <summary>Detach the bars first, so disposing a service does not update their widgets.</summary>
    public void Dispose()
    {
        network?.Dispose();
        audio?.Dispose();
        bluetooth?.Dispose();
        hyprland?.Dispose();
        weather?.Dispose();
        systemStatus?.Dispose();
        obsidian?.Dispose();
        clipboard?.Dispose();
        media?.Dispose();
        notifications?.Dispose();
        tray?.Dispose();
        kdeConnect?.Dispose();
        trash?.Dispose();
        calendar?.Dispose();
    }
}
