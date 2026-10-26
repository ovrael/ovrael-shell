namespace OvraelShell.Services;

/// <summary>Follows the trash. The values change on the main thread.</summary>
/// <remarks>
/// TODO: not implemented yet. The plan:
/// - Gio.File trash:/// - enumerate with trash::orig-path and trash::deletion-date (also covers .Trash-1000 on other mounts).
/// - Count on the bar from trash::item-count, changes from a FileMonitor on trash:///.
/// - Restore one item, or all items from one original folder (move the item to trash::orig-path, like gio trash --restore).
/// - Empty the trash only after a confirmation. Enumerate on Task.Run - large folders take time.
/// </remarks>
public sealed class TrashService : IDisposable
{
    public void Dispose() { }
}
