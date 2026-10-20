namespace OvraelShell.Models.Settings.Weather;

/// <param name="Index">Position of the entry in the settings file, from 1 - also of the incorrect entries.</param>
public sealed record WeatherLocationInfo(
    string Name,
    bool IsMain,
    double Latitude,
    double Longitude,
    int Index
)
{
    /// <summary>The name of a location without a name in the settings and from the geocoding.</summary>
    public string DefaultName => $"Location #{Index}";
}
