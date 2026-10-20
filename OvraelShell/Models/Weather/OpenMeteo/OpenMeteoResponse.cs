using System.Text.Json;
using System.Text.Json.Serialization;

namespace OvraelShell.Models.Weather.OpenMeteo;

// Shape of the response from api.open-meteo.com/v1/forecast. Only the requested sections
// and variables come in the response - the other ones are null.
// A value that the model does not have is null in the arrays too.

public sealed record OpenMeteoResponse(
    double Latitude,
    double Longitude,
    double GenerationtimeMs,
    int UtcOffsetSeconds,
    string Timezone,
    string TimezoneAbbreviation,
    double Elevation,
    IReadOnlyDictionary<string, string>? CurrentUnits,
    OpenMeteoCurrent? Current,
    IReadOnlyDictionary<string, string>? HourlyUnits,
    OpenMeteoHourly? Hourly,
    IReadOnlyDictionary<string, string>? DailyUnits,
    OpenMeteoDaily? Daily
)
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    /// <summary>
    /// One location - the API sends an object, more locations - an array of objects,
    /// in the same order as the coordinates in the request.
    /// </summary>
    /// <exception cref="JsonException">The text is not a correct response.</exception>
    public static IReadOnlyList<OpenMeteoResponse> Parse(string json)
    {
        var text = json.AsSpan().TrimStart();
        if (text.Length > 0 && text[0] == '[')
            return JsonSerializer.Deserialize<List<OpenMeteoResponse>>(json, jsonOptions) ?? [];

        var response = JsonSerializer.Deserialize<OpenMeteoResponse>(json, jsonOptions);
        return response is null ? [] : [response];
    }
}

// The time is the local time of the location, without an offset - see UtcOffsetSeconds.
// IsDay is 1 for the day and 0 for the night.

public sealed record OpenMeteoCurrent(
    DateTime Time,
    int Interval,
    int? WeatherCode,
    [property: JsonPropertyName("temperature_2m")] double? Temperature2m,
    int? PrecipitationProbability,
    int? IsDay
);

// The arrays have the same length - index i of each array is the hour Time[i]

public sealed record OpenMeteoHourly(
    IReadOnlyList<DateTime> Time,
    IReadOnlyList<int?>? WeatherCode,
    [property: JsonPropertyName("temperature_2m")] IReadOnlyList<double?>? Temperature2m,
    IReadOnlyList<int?>? PrecipitationProbability,
    IReadOnlyList<int?>? IsDay
);

// The arrays have the same length - index i of each array is the day Time[i]

public sealed record OpenMeteoDaily(
    IReadOnlyList<DateOnly> Time,
    IReadOnlyList<int?>? WeatherCode,
    [property: JsonPropertyName("temperature_2m_mean")] IReadOnlyList<double?>? Temperature2mMean,
    IReadOnlyList<int?>? PrecipitationProbabilityMean
);
