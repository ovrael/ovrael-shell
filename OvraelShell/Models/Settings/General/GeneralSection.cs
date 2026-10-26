using OvraelShell.Enums.Settings;

namespace OvraelShell.Models.Settings.General;

public sealed class GeneralSection
{
    public string? LanguageCode { get; set; } = "pl";

    public TimeFormat TimeFormat { get; set; } = TimeFormat.H24;

    /// <summary>DD, MM, YYYY style - see <see cref="GeneralSettings.DateFormatString"/>.</summary>
    public string? DateFormat { get; set; } = "DD-MM-YYYY";

    /// <summary>dark, light or system.</summary>
    public string? Theme { get; set; } = "dark";
}
