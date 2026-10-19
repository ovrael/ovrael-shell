using Gio;
using OvraelShell.Enums.Network;
using OvraelShell.Interfaces;
using OvraelShell.Utils;

namespace OvraelShell.Models.Network;

[GObject.Subclass<GObject.Object>]
public sealed partial class WeatherLocation
    : IReactivable<WeatherLocation>,
        IEquatable<WeatherLocation>
{
    public ReactiveProperty<string?> Name = new();
    public ReactiveProperty<bool> IsMain = new();
    public ReactiveProperty<double> Latitude = new();
    public ReactiveProperty<double> Longitude = new();

    public ReactiveProperty<double> Temperature = new();
    public ReactiveProperty<double> RainChance = new();

    public event Action<WeatherLocation>? OnChange;

    partial void Initialize()
    {
        Name.Changed += (_) => OnChange?.Invoke(this);
        IsMain.Changed += (_) => OnChange?.Invoke(this);
        Latitude.Changed += (_) => OnChange?.Invoke(this);
        Longitude.Changed += (_) => OnChange?.Invoke(this);
        Temperature.Changed += (_) => OnChange?.Invoke(this);
        RainChance.Changed += (_) => OnChange?.Invoke(this);
    }

    /// <summary>Creates a network for an access point, or null for a hidden network (no SSID).</summary>
    public static WeatherLocation? Create()
    {
        var network = NewWithProperties([]);

        return network;
    }

    #region Equality

    // Wrappers from GTK models (ListStore, ListItem) are compared by name or localization

    public bool Equals(WeatherLocation? other) =>
        other is not null
        && (Name == other.Name || (Latitude == other.Latitude && Longitude == other.Longitude));

    public override bool Equals(object? obj) => Equals(obj as WeatherLocation);

    public override int GetHashCode() => Name.GetHashCode();

    public static bool operator ==(WeatherLocation? left, WeatherLocation? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(WeatherLocation? left, WeatherLocation? right) =>
        !(left == right);

    #endregion

    public override string ToString() =>
        $"WeatherLocation {{ Name = {Name}, IsMain = {IsMain}, Latitude = {Latitude}, "
        + $"Longitude = {Longitude}, Temperature = {Temperature}, RainChance = {RainChance}%";
}
