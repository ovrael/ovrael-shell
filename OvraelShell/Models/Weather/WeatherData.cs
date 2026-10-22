using OvraelShell.Enums.Settings;
using OvraelShell.InterfaceElements;
using OvraelShell.Models.Weather.OpenMeteo;

namespace OvraelShell.Models.Weather;

/// <summary>
/// The weather at one time - now, one hour or one day. The time is the local time of the location.
/// A value that the API did not send is null.
/// </summary>
public sealed record WeatherData(
    DateTime Time,
    int? Code,
    bool IsDay,
    double? Temperature,
    int? PrecipitationProbability,
    TemperatureUnit Unit
)
{
    public string Icon => GetIcon(Code, IsDay).Icon;

    public string IconCssClass => GetIcon(Code, IsDay).CssClass;

    public string UnitSymbol => Unit == TemperatureUnit.F ? "°F" : "°C";

    public static WeatherData FromCurrent(OpenMeteoCurrent current, TemperatureUnit unit) =>
        new(
            current.Time,
            current.WeatherCode,
            current.IsDay != 0,
            current.Temperature2m,
            current.PrecipitationProbability,
            unit
        );

    public static IReadOnlyList<WeatherData> FromHourly(OpenMeteoHourly hourly, TemperatureUnit unit)
    {
        List<WeatherData> hours = [];
        for (var i = 0; i < hourly.Time.Count; i++)
        {
            hours.Add(
                new(
                    hourly.Time[i],
                    At(hourly.WeatherCode, i),
                    At(hourly.IsDay, i) != 0,
                    At(hourly.Temperature2m, i),
                    At(hourly.PrecipitationProbability, i),
                    unit
                )
            );
        }
        return hours;
    }

    // A day has no night - the day icons are used
    public static IReadOnlyList<WeatherData> FromDaily(OpenMeteoDaily daily, TemperatureUnit unit)
    {
        List<WeatherData> days = [];
        for (var i = 0; i < daily.Time.Count; i++)
        {
            days.Add(
                new(
                    daily.Time[i].ToDateTime(TimeOnly.MinValue),
                    At(daily.WeatherCode, i),
                    true,
                    At(daily.Temperature2mMean, i),
                    At(daily.PrecipitationProbabilityMean, i),
                    unit
                )
            );
        }
        return days;
    }

    // A variable that was not requested has no array, a shorter array has no value
    private static T? At<T>(IReadOnlyList<T?>? values, int index)
        where T : struct => values is not null && index < values.Count ? values[index] : null;

    // WMO weather interpretation codes - see "Weather variable documentation" on open-meteo.com
    // The CSS class gives the icon its color - see 020_Main.gtk.css
    private static (string Icon, string CssClass) GetIcon(int? code, bool isDay) =>
        code switch
        {
            0 => isDay
                ? (Icons.Weather.daySun, "weather-sun")
                : (Icons.Weather.nightMoon, "weather-moon"),
            1 or 2 => isDay
                ? (Icons.Weather.dayCloudyMid, "weather-cloud-mid")
                : (Icons.Weather.nightCloudyMid, "weather-cloud-mid-night"),
            3 or 45 or 48 => (Icons.Weather.cloudyFull, "weather-cloud-full"),
            (>= 51 and <= 67) or (>= 80 and <= 82) => isDay
                ? (Icons.Weather.dayRain, "weather-rain")
                : (Icons.Weather.nightRain, "weather-rain"),
            (>= 71 and <= 77) or 85 or 86 => isDay
                ? (Icons.Weather.daySnow, "weather-snow")
                : (Icons.Weather.nightSnow, "weather-snow"),
            >= 95 and <= 99 => isDay
                ? (Icons.Weather.dayThunderstorm, "weather-thunderstorm")
                : (Icons.Weather.nightThunderstorm, "weather-thunderstorm"),
            _ => (Icons.Weather.Other, "weather-unknown"),
        };

    public override string ToString() =>
        $"WeatherData {{ Time = {Time:g}, Code = {Code}, IsDay = {IsDay}, "
        + $"Temperature = {Temperature}{UnitSymbol}, PrecipitationProbability = {PrecipitationProbability}% }}";
}
