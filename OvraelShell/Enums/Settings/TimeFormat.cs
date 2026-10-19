using System.Text.Json.Serialization;

namespace OvraelShell.Enums.Settings;

// Values match the JSON number, so both "24" and 24 read correctly
public enum TimeFormat
{
    [JsonStringEnumMemberName("12")]
    H12 = 12,

    [JsonStringEnumMemberName("24")]
    H24 = 24,
}
