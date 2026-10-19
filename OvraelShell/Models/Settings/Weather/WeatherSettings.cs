using System.Globalization;
using OvraelShell.Enums.Settings;
using OvraelShell.Models.Settings.General;
using OvraelShell.Utils;

namespace OvraelShell.Models.Settings.Weather;

public sealed class WeatherSettings
{
    private static readonly WeatherSection defaults = new();

    public ReactiveProperty<TemperatureUnit> TemperatureUnit { get; } =
        new(ValidateTemperatureUnit(defaults.TemperatureUnit));

    public ReactiveProperty<bool> ShowLocationName { get; } = new(defaults.ShowLocationName!.Value);
    public ReactiveProperty<bool> AutogetLocalizationName { get; } =
        new(defaults.AutogetLocalizationName!.Value);
    public ReactiveProperty<IReadOnlyList<WeatherLocalizationInfo>> Localizations { get; } =
        new(
            ValidateLocalizations(defaults.Localizations, false),
            SequenceComparer<WeatherLocalizationInfo>.Instance
        );

    /// <summary>Takes the values from the file. An incorrect value is logged and the default is used.</summary>
    internal void Apply(WeatherSection section)
    {
        TemperatureUnit.Set(ValidateTemperatureUnit(section.TemperatureUnit));
        ShowLocationName.Set(CommonValidators.ValidateBool(section.ShowLocationName));
        AutogetLocalizationName.Set(CommonValidators.ValidateBool(section.ShowLocationName));

        Localizations.Set(ValidateLocalizations(section.Localizations, AutogetLocalizationName));
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

    private static IReadOnlyList<WeatherLocalizationInfo> ValidateLocalizations(
        IEnumerable<WeatherLocalizationEntry?>? entries,
        bool autogetLocalizationName
    )
    {
        List<WeatherLocalizationInfo> localizations = [];
        if (entries is null || !entries.Any())
            return localizations;

        int index = 1;
        foreach (var entry in entries)
        {
            var toAdd = ValidateLocalizationEntry(entry, index, autogetLocalizationName);
            if (toAdd is not null)
                localizations.Add(toAdd);
            else
            {
                Console.WriteLine($"Invalid localization with index: {index - 1}");
            }

            index++;
        }
        return localizations;
    }

    private static WeatherLocalizationInfo? ValidateLocalizationEntry(
        WeatherLocalizationEntry? entry,
        int index,
        bool autogetLocalizationName
    )
    {
        if (entry is null)
            return null;

        var coordinates = ValidateLocalization(entry);
        if (coordinates is null)
            return null;

        string name = string.Empty;
        if (string.IsNullOrEmpty(entry.Name))
        {
            name = autogetLocalizationName
                ? GetLocalizationName(coordinates.Value.latitude, coordinates.Value.longitude)
                : $"Localization #{index}";
        }
        else
        {
            name = entry.Name;
        }

        return new(
            name,
            CommonValidators.ValidateBool(entry.IsMain),
            coordinates.Value.latitude,
            coordinates.Value.longitude
        );
    }

    private static string GetLocalizationName(double latitude, double longitude)
    {
        return "";
    }

    private static (double latitude, double longitude)? ValidateLocalization(
        WeatherLocalizationEntry entry
    )
    {
        if (entry.Latitude is null || entry.Longitude is null)
            return null;

        if (
            entry.Latitude < -180
            || entry.Latitude > 180
            || entry.Longitude < -180
            || entry.Longitude > 180
        )
            return null;

        return (entry.Latitude.Value, entry.Longitude.Value);
    }
}
