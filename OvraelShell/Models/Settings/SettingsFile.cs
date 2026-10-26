using System.Text.Json.Serialization;
using OvraelShell.Models.Settings.General;
using OvraelShell.Models.Settings.Hyprland;
using OvraelShell.Models.Settings.Weather;
using OvraelShell.Models.Settings.Widgets;

namespace OvraelShell.Models.Settings;

// Shape of settings.json. The initial values are the defaults - also for a section
// or a key that is missing in the file.

public sealed class SettingsFile
{
    public GeneralSection? General { get; set; } = new();

    [JsonPropertyName("hyprworkspaces")]
    public HyprWorkspacesSection? HyprWorkspaces { get; set; } = new();

    [JsonPropertyName("weather")]
    public WeatherSection? Weather { get; set; } = new();

    public Dictionary<string, bool?>? WidgetsToLoad { get; set; } =
        WidgetsToLoadSettings.CreateDefaults();
}
