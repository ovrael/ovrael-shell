namespace OvraelShell.Models.Settings.Weather;

public sealed class WeatherLocalizationEntry
{
    public string? Name { get; set; }
    public bool? IsMain { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
