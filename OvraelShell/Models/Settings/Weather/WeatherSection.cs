using OvraelShell.Enums.Settings;

namespace OvraelShell.Models.Settings.Weather;

public class WeatherSection
{
    // "temperatureUnit": "C",
    // "showLocationName": true,
    // "autogetLocalizationName": true,
    // "localizations": [

    public string? TemperatureUnit { get; set; } = "C";
    public bool? ShowLocationName { get; set; } = true;
    public bool? AutogetLocalizationName { get; set; } = true;
    public List<WeatherLocalizationEntry?>? Localizations { get; set; } =
    [
        new()
        {
            Name = "Katowice",
            IsMain = true,
            Latitude = 50.25,
            Longitude = 19.0,
        },
    ];
}
