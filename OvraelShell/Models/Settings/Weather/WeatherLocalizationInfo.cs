namespace OvraelShell.Models.Settings.Weather;

public sealed record WeatherLocalizationInfo(
    string Name,
    bool IsMain,
    double Latitude,
    double Longitude
);
