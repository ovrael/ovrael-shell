using System.Globalization;
using System.Text.RegularExpressions;
using OvraelShell.Enums.Settings;

namespace OvraelShell.Models.Settings.General;

public sealed class GeneralSettings
{
    private static readonly GeneralSection defaults = new();

    public ReactiveProperty<string> LanguageCode { get; } = new(defaults.LanguageCode!);

    public ReactiveProperty<TimeFormat> TimeFormat { get; } = new(defaults.TimeFormat);

    public ReactiveProperty<string> DateFormat { get; } = new(defaults.DateFormat!);

    /// <summary>The theme in the file - the current theme is <see cref="Utils.Theme.ThemeManager.UserTheme"/>.</summary>
    public ReactiveProperty<ThemeScheme> Theme { get; } = new(ValidTheme(defaults.Theme));

    /// <summary>.NET format of <see cref="TimeFormat"/>.</summary>
    public string TimeFormatString =>
        TimeFormat.Value == Enums.Settings.TimeFormat.H12 ? "hh:mm:ss tt" : "HH:mm:ss";

    /// <summary>.NET format of <see cref="DateFormat"/> - DD-MM-YYYY becomes dd-MM-yyyy.</summary>
    public string DateFormatString => ToDotNetDateFormat(DateFormat.Value);

    /// <summary>
    /// <see cref="DateFormatString"/> without the year and its separator - dd-MM-yyyy becomes dd-MM,
    /// yyyy-MM-dd becomes MM-dd. For short dates, like the days of the weather forecast.
    /// </summary>
    public string DayMonthFormatString =>
        Regex.Replace(DateFormatString, "^y+[^dM]*|[^dM]*y+", string.Empty);

    /// <summary>Takes the values from the file. An incorrect value is logged and the default is used.</summary>
    internal void Apply(GeneralSection section)
    {
        LanguageCode.Set(ValidLanguageCode(section.LanguageCode));
        TimeFormat.Set(ValidTimeFormat(section.TimeFormat));
        DateFormat.Set(ValidDateFormat(section.DateFormat));
        Theme.Set(ValidTheme(section.Theme));
    }

    private static string ValidLanguageCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return defaults.LanguageCode!;

        try
        {
            // Only predefined - otherwise ICU accepts any well-formed tag
            return CultureInfo.GetCultureInfo(code, predefinedOnly: true).Name;
        }
        catch (CultureNotFoundException)
        {
            Console.WriteLine($"Settings: unknown languageCode \"{code}\", using the default.");
            return defaults.LanguageCode!;
        }
    }

    private static TimeFormat ValidTimeFormat(TimeFormat format)
    {
        if (Enum.IsDefined(format))
            return format;

        Console.WriteLine($"Settings: unknown timeFormat \"{(int)format}\", using the default.");
        return defaults.TimeFormat;
    }

    // A string, not the enum - an incorrect value in the file must not stop the whole file
    private static ThemeScheme ValidTheme(string? theme)
    {
        if (string.IsNullOrWhiteSpace(theme))
            return ThemeScheme.Dark;

        // Only the names - Enum.TryParse also accepts numbers
        foreach (var scheme in Enum.GetValues<ThemeScheme>())
        {
            if (string.Equals(scheme.ToString(), theme.Trim(), StringComparison.OrdinalIgnoreCase))
                return scheme;
        }

        Console.WriteLine($"Settings: unknown theme \"{theme}\", using the default.");
        return ThemeScheme.Dark;
    }

    private static string ValidDateFormat(string? format)
    {
        if (string.IsNullOrWhiteSpace(format))
            return defaults.DateFormat!;

        try
        {
            _ = DateTime.Now.ToString(ToDotNetDateFormat(format), CultureInfo.InvariantCulture);
            return format;
        }
        catch (FormatException)
        {
            Console.WriteLine($"Settings: incorrect dateFormat \"{format}\", using the default.");
            return defaults.DateFormat!;
        }
    }

    // In .NET day and year are lower case, month (MM) is the same
    private static string ToDotNetDateFormat(string format) =>
        format.Replace('D', 'd').Replace('Y', 'y');
}
