namespace OvraelShell.Services;

/// <summary>Follows the tray icons of the applications, the removable drives and the background services. The values change on the main thread.</summary>
/// <remarks>
/// TODO: not implemented yet. The plan:
/// - Apps: StatusNotifierItem: the shell is the org.kde.StatusNotifierWatcher and the host (none runs now, so the Steam icon is not shown anywhere).
/// - Apps: Menus of the items through com.canonical.dbusmenu.
/// - Drives: UDisks2 on the system bus: removable block devices (pendrives, cards).
/// - Drives: Actions: Filesystem.Mount, Filesystem.Unmount, Drive.PowerOff / Eject.
/// - Services: List from the settings: name, how to check the state, how to start and stop it.
/// - Services: systemd user units through org.freedesktop.systemd1, other programs by process (Steam).
/// </remarks>
public sealed class TrayService : IDisposable
{
    public void Dispose() { }
}
