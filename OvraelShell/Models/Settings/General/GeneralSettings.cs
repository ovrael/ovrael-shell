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
