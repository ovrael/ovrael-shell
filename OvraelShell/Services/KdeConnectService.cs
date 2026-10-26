namespace OvraelShell.Services;

/// <summary>Follows the devices of KDE Connect. The values change on the main thread.</summary>
/// <remarks>
/// TODO: not implemented yet. The plan:
/// - kdeconnectd on the session bus: org.kde.kdeconnect, /modules/kdeconnect - devices(onlyReachable, onlyPaired).
/// - Per device /modules/kdeconnect/devices/&lt;id&gt;: name, isReachable, battery (charge, isCharging).
/// - Actions: ping, find the phone (ring), share a file or a URL, send the clipboard.
/// </remarks>
public sealed class KdeConnectService : IDisposable
{
    public void Dispose() { }
}
