namespace OvraelShell.Services;

/// <summary>Receives the notifications of the applications and keeps their history. The values change on the main thread.</summary>
/// <remarks>
/// TODO: not implemented yet. The plan:
/// - No notification daemon runs now - the shell must own org.freedesktop.Notifications (Gio.BusOwnName).
/// - Methods: Notify, CloseNotification, GetCapabilities, GetServerInformation; signals: NotificationClosed, ActionInvoked.
/// - History with a limit, saved in $XDG_STATE_HOME; popups in a separate layer-shell window.
/// - Do not disturb mode. If another daemon owns the name, show that the history is not available.
/// </remarks>
public sealed class NotificationsService : IDisposable
{
    public void Dispose() { }
}
