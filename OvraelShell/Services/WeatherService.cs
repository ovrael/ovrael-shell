using System.Globalization;
using System.Text.Json;
using OvraelShell.Enums.Settings;
using OvraelShell.Models;
using OvraelShell.Models.Settings.Weather;
using OvraelShell.Models.Weather;
using OvraelShell.Models.Weather.OpenMeteo;
using OvraelShell.Utils.Settings;

namespace OvraelShell.Services;

/// <summary>
/// The weather for the locations from the settings. The locations follow the settings file -
/// a change sends a new request at once.
/// </summary>
public sealed class WeatherService : IDisposable
{
    private const uint INTERVAL_MS = 10 * 60_000;

    // Nominatim usage policy: at most 1 request per second
    private const int GEOCODING_DELAY_MS = 1100;

    // Longer names - from the settings or the geocoding - are cut, so the bar and the tabs stay short
    private const int MAX_NAME_LENGTH = 30;

    private uint timeoutId;
    public bool IsRunning => timeoutId != 0;

    private CancellationTokenSource? updateCancellation;
    private CancellationTokenSource? geocodingCancellation;

    private readonly HttpClient openMeteoClient;
    private readonly HttpClient geocodingClient;

    // Names from the reverse geocoding - a location is not looked up again after a settings reload or a restart
    private readonly LocationNamesCache namesCache = new();

    public ReactiveCollection<WeatherForecast> Locations { get; } = new();
    public ReactiveProperty<WeatherForecast?> MainLocation { get; } = new();

    public WeatherService()
    {
        openMeteoClient = new HttpClient()
        {
            BaseAddress = new Uri("https://api.open-meteo.com/v1/forecast"),
        };

        // Nominatim refuses requests without an application name
        geocodingClient = new HttpClient()
        {
            BaseAddress = new Uri("https://nominatim.openstreetmap.org/reverse"),
        };
        geocodingClient.DefaultRequestHeaders.UserAgent.ParseAdd("OvraelShell");

        // The service lives as long as the application, like the static settings
        UserSettings.Weather.Locations.Changed += OnLocationsChanged;
        UserSettings.Weather.TemperatureUnit.Changed += OnTemperatureUnitChanged;

        // The names are in the language of the settings - another language finds them again
        UserSettings.General.LanguageCode.Changed += OnLanguageChanged;
        namesCache.Changed += OnNamesCacheChanged;

        SyncLocations(UserSettings.Weather.Locations.Value);
        RestartUpdate();

        timeoutId = GLib.Functions.TimeoutAdd(0, INTERVAL_MS, UpdateWeather);
    }

    private static (double, double) KeyOf(WeatherLocationInfo info) =>
        (info.Latitude, info.Longitude);

    private static (double, double) KeyOf(WeatherForecast location) =>
        (location.Latitude, location.Longitude);

    private void OnLocationsChanged(IReadOnlyList<WeatherLocationInfo> infos)
    {
        SyncLocations(infos);
        RestartUpdate();
    }

    private void OnTemperatureUnitChanged(TemperatureUnit _) => RestartUpdate();

    // UserSettings subscribed first, so the culture is already the new one
    private void OnLanguageChanged(string _) => SyncLocations(UserSettings.Weather.Locations.Value);

    // The user changed the names, or deleted them - they are looked up again
    private void OnNamesCacheChanged() => SyncLocations(UserSettings.Weather.Locations.Value);

    // The language of the names from Nominatim, and of their key in the cache
    private static string NamesLanguage => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

    private void SyncLocations(IReadOnlyList<WeatherLocationInfo> infos)
    {
        // Two entries with the same coordinates are one location - the first one is used
        var byKey = infos.DistinctBy(KeyOf).ToDictionary(KeyOf);

        // Locations with the same coordinates keep their object and their last weather
        Locations.Sync(
            byKey.Keys,
            KeyOf,
            key =>
                WeatherForecast.Create(byKey[key].Name, byKey[key].Latitude, byKey[key].Longitude)
        );

        var language = NamesLanguage;
        List<WeatherForecast> withoutName = [];
        foreach (var location in Locations.Items)
        {
            var key = KeyOf(location);
            var info = byKey[key];
            var name = info.Name;

            // An empty name from the settings - the name is found from the coordinates
            if (string.IsNullOrEmpty(name) && UserSettings.Weather.AutogetLocationName.Value)
            {
                if (namesCache.TryGet(location.Latitude, location.Longitude, language, out var cachedName))
                    name = cachedName;
                else
                    withoutName.Add(location);
            }

            // Until the geocoding finds the name, or when it cannot
            location.Name.Set(LimitName(name.Length > 0 ? name : info.DefaultName));
        }

        var main = infos.FirstOrDefault(i => i.IsMain) ?? infos.FirstOrDefault();
        MainLocation.Set(main is null ? null : Locations.Items.First(l => KeyOf(l) == KeyOf(main)));

        geocodingCancellation?.Cancel();
        geocodingCancellation = null;
        if (withoutName.Count > 0)
        {
            geocodingCancellation = new CancellationTokenSource();
            _ = FillNamesAsync(withoutName, language, geocodingCancellation.Token);
        }
    }

    private void StopTimeout()
    {
        if (!IsRunning)
            return;

        GLib.Functions.SourceRemove(timeoutId);
        timeoutId = 0;
    }

    private bool UpdateWeather()
    {
        RestartUpdate();

        // Keep the timeout
        return true;
    }

    // A new request replaces the running one - its locations or unit can be out of date
    private void RestartUpdate()
    {
        updateCancellation?.Cancel();
        updateCancellation = new CancellationTokenSource();

        // The request is not awaited here, so the main loop is not blocked
        _ = UpdateWeatherAsync(updateCancellation.Token);
    }

    private async Task UpdateWeatherAsync(CancellationToken token)
    {
        // The response has the same order as the coordinates in the request
        var requested = Locations.Items.ToArray();
        if (requested.Length == 0)
            return;

        // The unit of the request - a change of the settings sends a new one
        var unit = UserSettings.Weather.TemperatureUnit.Value;

        // The application runs with the GLib synchronization context,
        // so the code after await runs on the main thread again
        var content = await SendRequest(requested, unit, token);
        if (string.IsNullOrEmpty(content) || token.IsCancellationRequested)
            return;

        UpdateForecast(requested, content, unit);
    }

    private static void UpdateForecast(
        IReadOnlyList<WeatherForecast> requested,
        string content,
        TemperatureUnit unit
    )
    {
        IReadOnlyList<OpenMeteoResponse> responses;
        try
        {
            responses = OpenMeteoResponse.Parse(content);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Weather: cannot read the response from Open-Meteo: {ex.Message}");
            return;
        }

        // Index i of the response is location i of the request - with other counts they do not match
        if (responses.Count != requested.Count)
        {
            Console.WriteLine(
                $"Weather: {responses.Count} forecasts for {requested.Count} locations, the response is ignored"
            );
            return;
        }

        for (var i = 0; i < requested.Count; i++)
            requested[i].Update(responses[i], unit);
    }

    private async Task<string?> SendRequest(
        IReadOnlyList<WeatherForecast> requested,
        TemperatureUnit unit,
        CancellationToken token
    )
    {
        try
        {
            // Invariant culture - with a Polish culture "50,25" would split into two coordinates
            var latitudes = string.Join(
                ",",
                requested.Select(l => l.Latitude.ToString(CultureInfo.InvariantCulture))
            );
            var longitudes = string.Join(
                ",",
                requested.Select(l => l.Longitude.ToString(CultureInfo.InvariantCulture))
            );
            var currentParams =
                "&current=weather_code,temperature_2m,precipitation_probability,is_day";
            var hourlyParams =
                "&forecast_hours=21&past_hours=0&hourly=weather_code,temperature_2m,precipitation_probability,is_day"; // STARTS FROM CURRENT HOUR
            var dailyParams =
                "&daily=weather_code,temperature_2m_mean,precipitation_probability_mean"; // STARTS FROM CURRENT DAY
            var tempUnit = unit == TemperatureUnit.F ? "fahrenheit" : "celsius";
            var tempUnitParam = $"&temperature_unit={tempUnit}";
            var timezone = "&timezone=auto";

            var query =
                $"?latitude={latitudes}&longitude={longitudes}{currentParams}{hourlyParams}{dailyParams}{tempUnitParam}{timezone}";

            using var response = await openMeteoClient.GetAsync(query, token);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync(token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Replaced by a newer request, or disposed
        }
        catch (Exception ex)
        {
            Console.WriteLine("Cannnot get weather from OPEN METEO API! " + ex);
        }
        return null;
    }

    private static string LimitName(string name)
    {
        if (name.Length <= MAX_NAME_LENGTH)
            return name;

        // The ellipsis is the last of the MAX_NAME_LENGTH characters
        var length = MAX_NAME_LENGTH - 1;

        // Do not cut a character from outside the BMP (two UTF-16 units) in half
        if (char.IsHighSurrogate(name[length - 1]))
            length--;

        return name[..length].TrimEnd() + "…";
    }

    // One request at a time, with a delay between them - see GEOCODING_DELAY_MS
    private async Task FillNamesAsync(
        IReadOnlyList<WeatherForecast> withoutName,
        string language,
        CancellationToken token
    )
    {
        try
        {
            for (var i = 0; i < withoutName.Count; i++)
            {
                if (i > 0)
                    await Task.Delay(GEOCODING_DELAY_MS, token);

                var location = withoutName[i];
                var name = await GetLocationName(
                    location.Latitude,
                    location.Longitude,
                    language,
                    token
                );
                if (name is null || token.IsCancellationRequested)
                    continue;

                namesCache.Set(location.Latitude, location.Longitude, language, name);
                location.Name.Set(LimitName(name));
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // The settings changed, or disposed
        }
    }

    private async Task<string?> GetLocationName(
        double latitude,
        double longitude,
        string language,
        CancellationToken token
    )
    {
        try
        {
            // Zoom 10 - the name of the city, not of the street
            var query = string.Create(
                CultureInfo.InvariantCulture,
                $"?format=jsonv2&zoom=10&lat={latitude}&lon={longitude}"
                    + $"&accept-language={language}"
            );

            using var response = await geocodingClient.GetAsync(query, token);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(token);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: token);

            foreach (var property in new[] { "name", "display_name" })
            {
                if (
                    json.RootElement.TryGetProperty(property, out var value)
                    && value.GetString() is { Length: > 0 } name
                )
                    return name;
            }
        }
        // OperationCanceledException without a cancelled token - the HttpClient timeout
        catch (Exception ex)
            when (ex is HttpRequestException or JsonException
                || (ex is OperationCanceledException && !token.IsCancellationRequested)
            )
        {
            Console.WriteLine(
                $"Weather: cannot get the name of {latitude}, {longitude}: {ex.Message}"
            );
        }
        return null;
    }

    public void Dispose()
    {
        UserSettings.Weather.Locations.Changed -= OnLocationsChanged;
        UserSettings.Weather.TemperatureUnit.Changed -= OnTemperatureUnitChanged;
        UserSettings.General.LanguageCode.Changed -= OnLanguageChanged;
        namesCache.Changed -= OnNamesCacheChanged;
        namesCache.Dispose();

        updateCancellation?.Cancel();
        geocodingCancellation?.Cancel();
        StopTimeout();

        openMeteoClient.Dispose();
        geocodingClient.Dispose();
    }
}
