using System.Text.Json.Serialization;
using OvraelShell.Enums.Common;

namespace OvraelShell.Models.Settings;

// Shape of settings.json. The initial values are the defaults - also for a section
// or a key that is missing in the file.

public sealed class SettingsFile
{
    public GeneralSection? General { get; set; } = new();

    [JsonPropertyName("hyprworkspaces")]
    public HyprWorkspacesSection? HyprWorkspaces { get; set; } = new();
}

public sealed class GeneralSection
{
    public string? LanguageCode { get; set; } = "pl";

    public TimeFormat TimeFormat { get; set; } = TimeFormat.H24;

    /// <summary>DD, MM, YYYY style - see <see cref="GeneralSettings.DateFormatString"/>.</summary>
    public string? DateFormat { get; set; } = "DD-MM-YYYY";
}

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

public sealed class WorkspaceEntry
{
    public string? Name { get; set; }

    public string? Icon { get; set; }
}
