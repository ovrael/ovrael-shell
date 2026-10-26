namespace OvraelShell.Services;

/// <summary>Gives the daily note of the Obsidian vault. The values change on the main thread.</summary>
/// <remarks>
/// TODO: not implemented yet. The plan:
/// - Vault from the settings (default: the open vault in ~/.config/obsidian/obsidian.json).
/// - Folder, date format and template from &lt;vault&gt;/.obsidian/daily-notes.json (no file: vault root, YYYY-MM-DD, no template).
/// - Open the note with obsidian://open?vault=...&amp;file=... (Gio.AppInfo.LaunchDefaultForUri).
/// - Quick entry: append a line or a task (- [ ] ...) to today's note, create it from the template if missing.
/// - The vault is on an NFS hard mount: all file IO on Task.Run with a timeout, never on the main thread.
/// </remarks>
public sealed class ObsidianService : IDisposable
{
    public void Dispose() { }
}
