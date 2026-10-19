namespace OvraelShell.Models.Settings.Hyprland;

public sealed class HyprWorkspacesSection
{
    public List<WorkspaceEntry?>? Workspaces { get; set; } =
    [
        new() { Name = "main", Icon = "\uee15" },
        new() { Name = "gaming", Icon = "\uf11b" },
        new() { Name = "coding", Icon = "\uf121" },
        new() { Name = "terminal", Icon = "\uf120" },
        new() { Name = "media", Icon = "\uf001" },
    ];
}
