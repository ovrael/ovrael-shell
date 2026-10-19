namespace OvraelShell.Models.Settings.Hyprland;

/// <summary>A workspace from the settings. Empty strings mean no name or no icon.</summary>
public sealed record WorkspaceInfo(string Name, string Icon);
