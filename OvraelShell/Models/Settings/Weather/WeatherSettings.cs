using System.Globalization;
using OvraelShell.Enums.Settings;
using OvraelShell.Models.Settings.General;
using OvraelShell.Utils;

namespace OvraelShell.Models.Settings.Weather;

public sealed class WeatherSettings
{
    public const int MaxLocations = 10;

    private static readonly WeatherSection defaults = new();

    public ReactiveProperty<TemperatureUnit> TemperatureUnit { get; } =
        new(ValidateTemperatureUnit(defaults.TemperatureUnit));

    public ReactiveProperty<bool> ShowLocationName { get; } = new(defaults.ShowLocationName!.Value);
    public ReactiveProperty<bool> AutogetLocationName { get; } =
        new(defaults.AutogetLocationName!.Value);
    public ReactiveProperty<IReadOnlyList<WeatherLocationInfo>> Locations { get; } =
        new(
            ValidateLocations(defaults.Locations, false),
            SequenceComparer<WeatherLocationInfo>.Instance
        );

    /// <summary>Takes the values from the file. An incorrect value is logged and the default is used.</summary>
    internal void Apply(WeatherSection section)
    {
        TemperatureUnit.Set(ValidateTemperatureUnit(section.TemperatureUnit));
        ShowLocationName.Set(CommonValidators.ValidateBool(section.ShowLocationName));
        AutogetLocationName.Set(CommonValidators.ValidateBool(section.AutogetLocationName));

        Locations.Set(ValidateLocations(section.Locations, AutogetLocationName.Value));
    }

    private static TemperatureUnit ValidateTemperatureUnit(string? temperatureUnit)
    {
        if (string.IsNullOrEmpty(temperatureUnit))
            return Enums.Settings.TemperatureUnit.C;

        char first = temperatureUnit[0];
        if (first == 'F' || first == 'f')
            return Enums.Settings.TemperatureUnit.F;
        return Enums.Settings.TemperatureUnit.C;
    }

    private static IReadOnlyList<WeatherLocationInfo> ValidateLocations(
        IEnumerable<WeatherLocationEntry?>? entries,
        bool autogetLocationName
    )
    {
        List<WeatherLocationInfo> locations = [];
        if (entries is null || !entries.Any())
            return locations;

        int index = 1;
        foreach (var entry in entries)
        {
            var toAdd = ValidateLocationEntry(entry, index, autogetLocationName);
            if (toAdd is null)
            {
                Console.WriteLine($"Invalid location entry with index: {index - 1}");
            }
            else if (locations.Count == MaxLocations)
            {
                Console.WriteLine(
                    $"Weather: more than {MaxLocations} locations, the entry with index {index - 1} is ignored"
                );
            }
            else
            {
                locations.Add(toAdd);
            }

            index++;
        }
        return locations;
    }

    private static WeatherLocationInfo? ValidateLocationEntry(
        WeatherLocationEntry? entry,
        int index,
        bool autogetLocationName
    )
    {
        if (entry is null)
            return null;

        var coordinates = ValidateLocation(entry);
        if (coordinates is null)
            return null;

        WeatherLocationInfo info = new(
            entry.Name ?? string.Empty,
            CommonValidators.ValidateBool(entry.IsMain),
            coordinates.Value.latitude,
            coordinates.Value.longitude,
            index
        );

        // An empty name stays empty - WeatherService finds it from the coordinates.
        // It is a network request, so it is not done here, on each reload of the file.
        return info.Name.Length == 0 && !autogetLocationName
            ? info with { Name = info.DefaultName }
            : info;
    }

    private static (double latitude, double longitude)? ValidateLocation(WeatherLocationEntry entry)
    {
        if (entry.Latitude is null || entry.Longitude is null)
            return null;

        if (
            entry.Latitude < -90
            || entry.Latitude > 90
            || entry.Longitude < -180
            || entry.Longitude > 180
        )
            return null;

        return (entry.Latitude.Value, entry.Longitude.Value);
    }
}
