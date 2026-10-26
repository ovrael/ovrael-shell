namespace OvraelShell.Services;

/// <summary>Gives the clipboard history. The values change on the main thread.</summary>
/// <remarks>
/// TODO: not implemented yet. The plan:
/// - Source: cliphist (wl-paste --watch cliphist store) - list with 'cliphist list', copy again with 'cliphist decode &lt;id&gt; | wl-copy'.
/// - Without an external daemon: the ext-data-control-v1 Wayland protocol (more work).
/// - Do not keep entries of password managers (x-kde-passwordManagerHint: secret).
/// - Image entries: a preview from 'cliphist decode' as a Gdk.Texture.
/// </remarks>
public sealed class ClipboardService : IDisposable
{
    public void Dispose() { }
}
