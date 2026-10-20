using OvraelShell.Enums.Settings;

namespace OvraelShell.Models.Settings.Weather;

public class WeatherSection
{
    public string? TemperatureUnit { get; set; } = "C";
    public bool? ShowLocationName { get; set; } = true;
    public bool? AutogetLocationName { get; set; } = true;
    public List<WeatherLocationEntry?>? Locations { get; set; } =
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
