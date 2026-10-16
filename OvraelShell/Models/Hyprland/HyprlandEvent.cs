namespace OvraelShell.Models.Hyprland;

/// <summary>One line of the event socket: NAME&gt;&gt;DATA.</summary>
public readonly record struct HyprlandEvent(string Name, string Data);
