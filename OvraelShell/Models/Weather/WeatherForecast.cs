using OvraelShell.Enums.Settings;
using OvraelShell.Interfaces;
using OvraelShell.Models.Weather.OpenMeteo;
using OvraelShell.Utils;

namespace OvraelShell.Models.Weather;

/// <summary>
/// A location from the settings and its weather. The coordinates do not change -
/// other coordinates in the settings make a new object (see WeatherService.SyncLocations).
/// </summary>
[GObject.Subclass<GObject.Object>]
public sealed partial class WeatherForecast
    : IReactivable<WeatherForecast>,
        IEquatable<WeatherForecast>
{
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }

    public ReactiveProperty<string> Name { get; } = new(string.Empty);

    // Null until the first response
    public ReactiveProperty<WeatherData?> Current { get; } = new();

    // Starts from the current hour
    public ReactiveProperty<IReadOnlyList<WeatherData>> Hourly { get; } =
        new([], SequenceComparer<WeatherData>.Instance);

    // Starts from the current day
    public ReactiveProperty<IReadOnlyList<WeatherData>> Daily { get; } =
        new([], SequenceComparer<WeatherData>.Instance);

    public event Action<WeatherForecast>? OnChange;

    partial void Initialize()
    {
        Name.Changed += (_) => OnChange?.Invoke(this);
        Current.Changed += (_) => OnChange?.Invoke(this);
        Hourly.Changed += (_) => OnChange?.Invoke(this);
        Daily.Changed += (_) => OnChange?.Invoke(this);
    }

    public static WeatherForecast Create(string name, double latitude, double longitude)
    {
        var forecast = NewWithProperties([]);
        forecast.Latitude = latitude;
        forecast.Longitude = longitude;
        forecast.Name.Set(name);
        return forecast;
    }

    /// <summary>Takes the weather from the response. A section that is missing keeps the old values.</summary>
    public void Update(OpenMeteoResponse response, TemperatureUnit unit)
    {
        if (response.Current is { } current)
            Current.Set(WeatherData.FromCurrent(current, unit));

        if (response.Hourly is { } hourly)
            Hourly.Set(WeatherData.FromHourly(hourly, unit));

        if (response.Daily is { } daily)
            Daily.Set(WeatherData.FromDaily(daily, unit));
    }

    #region Equality

    // Wrappers from GTK models (ListStore, ListItem) are compared by the coordinates -
    // they do not change, the name can (reverse geocoding)

    public bool Equals(WeatherForecast? other) =>
        other is not null && Latitude == other.Latitude && Longitude == other.Longitude;

    public override bool Equals(object? obj) => Equals(obj as WeatherForecast);

    public override int GetHashCode() => HashCode.Combine(Latitude, Longitude);

    public static bool operator ==(WeatherForecast? left, WeatherForecast? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(WeatherForecast? left, WeatherForecast? right) =>
        !(left == right);

    #endregion

    public override string ToString() =>
        $"WeatherForecast {{ Name = {Name}, Latitude = {Latitude}, Longitude = {Longitude}, "
        + $"Current = {Current}, Hourly = {Hourly.Value.Count}, Daily = {Daily.Value.Count} }}";
}
