namespace OvraelShell.Models.Settings.Weather;

/// <summary>
/// A name from the reverse geocoding, in location-names.json next to the settings.
/// The user can change it - all values can be missing or incorrect after that.
/// </summary>
public sealed class LocationNameEntry
{
    public string? Name { get; set; }

    // The names from Nominatim are in the language of the request
    public string? Language { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
