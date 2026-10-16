using OvraelShell.Utils;

namespace OvraelShell.Models.Settings;

public sealed class HyprWorkspacesSettings
{
    private static readonly HyprWorkspacesSection defaults = new();

    /// <summary>The workspaces - the first one is workspace 1.</summary>
    public ReactiveProperty<IReadOnlyList<WorkspaceInfo>> Workspaces { get; } =
        new(ToWorkspaces(defaults.Workspaces!), SequenceComparer<WorkspaceInfo>.Instance);

    internal void Apply(HyprWorkspacesSection section) =>
        Workspaces.Set(ToWorkspaces(section.Workspaces ?? defaults.Workspaces!));

    // A null entry or value keeps its place - the next workspaces keep their numbers
    private static WorkspaceInfo[] ToWorkspaces(IEnumerable<WorkspaceEntry?> entries) =>
        entries
            .Select(entry => new WorkspaceInfo(
                entry?.Name?.Trim() ?? string.Empty,
                entry?.Icon?.Trim() ?? string.Empty
            ))
            .ToArray();
}
