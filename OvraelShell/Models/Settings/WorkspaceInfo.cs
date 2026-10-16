namespace OvraelShell.Models.Settings;

/// <summary>A workspace from the settings. Empty strings mean no name or no icon.</summary>
public sealed record WorkspaceInfo(string Name, string Icon);
