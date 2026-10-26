namespace OvraelShell.Services;

/// <summary>Follows the media players. The values change on the main thread.</summary>
/// <remarks>
/// TODO: not implemented yet. The plan:
/// - MPRIS on the session bus: the org.mpris.MediaPlayer2.* names, follow NameOwnerChanged.
/// - Per player: PlaybackStatus, Metadata (title, artist, mpris:artUrl), CanGoNext/CanGoPrevious.
/// - Position has no change signal - poll it only while the popover is open.
/// - Actions: PlayPause, Next, Previous, SetPosition.
/// - Skip playerctld - it only repeats the other players.
/// </remarks>
public sealed class MediaService : IDisposable
{
    public void Dispose() { }
}
