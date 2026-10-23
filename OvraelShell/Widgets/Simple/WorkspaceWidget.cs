using Gtk;
using OvraelShell.InterfaceElements;
using OvraelShell.Models.Settings;
using OvraelShell.Models.Settings.Hyprland;
using OvraelShell.Services;
using OvraelShell.Utils.Settings;

namespace OvraelShell.Widgets.Simple;

/// <summary>
/// The workspaces from the settings - their icons, and the name of the active one.
/// An active workspace without settings shows as "Other".
/// </summary>
[GObject.Subclass<Gtk.Box>]
public partial class WorkspaceWidget
{
    private static readonly WorkspaceInfo other = new("Other", Icons.Workspace.Other);

    // Shared by all bars and owned by Program. Set in New, before the widget is mapped.
    private HyprlandService hyprland = null!;

    public static WorkspaceWidget New(HyprlandService hyprland)
    {
        var widget = NewWithProperties([]);
        widget.hyprland = hyprland;
        widget.UpdateLabels();
        return widget;
    }

    partial void Initialize()
    {
        SetOrientation(Orientation.Horizontal);
        AddCssClass("workspaces");

        // The settings are static and the service outlives the bar - follow them only
        // while shown, so they do not keep the widget alive
        OnMap += (_, _) =>
        {
            UserSettings.HyprWorkspaces.Workspaces.Changed += OnWorkspacesChanged;
            hyprland.ActiveWorkspaceId.Changed += OnActiveWorkspaceChanged;
            UpdateLabels();
        };
        OnUnmap += (_, _) =>
        {
            UserSettings.HyprWorkspaces.Workspaces.Changed -= OnWorkspacesChanged;
            hyprland.ActiveWorkspaceId.Changed -= OnActiveWorkspaceChanged;
        };
    }

    private void OnWorkspacesChanged(IReadOnlyList<WorkspaceInfo> _) => UpdateLabels();

    private void OnActiveWorkspaceChanged(int? _) => UpdateLabels();

    // A few labels - simpler to create them again than to update them
    private void UpdateLabels()
    {
        while (GetFirstChild() is { } child)
            Remove(child);

        var workspaces = UserSettings.HyprWorkspaces.Workspaces.Value;
        var activeId = hyprland.ActiveWorkspaceId.Value;

        // Workspace ids start at 1, the first one in the settings is workspace 1
        for (var i = 0; i < workspaces.Count; i++)
            Append(CreateLabel(workspaces[i], i + 1, activeId == i + 1));

        // Special workspaces have negative ids
        if (activeId is { } id && (id < 1 || id > workspaces.Count))
            Append(CreateLabel(other, id, true));
    }

    // Without an icon the number shows
    private static Label CreateLabel(WorkspaceInfo workspace, int id, bool isActive)
    {
        var icon = workspace.Icon.Length > 0 ? workspace.Icon : $"{id}";
        var text = isActive ? $"{icon} {workspace.Name}".TrimEnd() : icon;

        var label = Label.New(text);
        label.AddCssClass("workspace");

        if (isActive)
            label.AddCssClass("active");

        return label;
    }
}
