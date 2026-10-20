using System.Globalization;
using Gtk;
using OvraelShell.Enums.Settings;
using OvraelShell.InterfaceElements;
using OvraelShell.Models.Weather;
using OvraelShell.Utils;
using OvraelShell.Utils.Settings;

namespace OvraelShell.Widgets.Weather.Elements;

/// <summary>
/// One tab of the weather popover. On the left the weather now, on the right the next hours
/// (they scroll) and below them the next days - the days give the width of the page.
/// The page follows the forecast until <see cref="Dispose"/>.
/// </summary>
public sealed class WeatherLocationPage : IDisposable
{
    // The response has 21 hours, from the current one
    private const int HourStep = 2;

    public WeatherForecast Forecast { get; }
    public Box View { get; }

    private readonly Label currentIcon;
    private readonly Label currentTemperature;
    private readonly Label currentPrecipitation;
    private readonly Label emptyLabel;
    private readonly ScrolledWindow hourlyScroll;
    private readonly Box hourly;
    private readonly Box daily;

    public WeatherLocationPage(WeatherForecast forecast)
    {
        Forecast = forecast;

        View = Box.New(Orientation.Horizontal, 16);

        // Left: now
        currentIcon = Label.New(Icons.Weather.Other);
        currentIcon.AddCssClass("weather-current-icon");

        currentTemperature = Label.New(string.Empty);
        currentTemperature.AddCssClass("weather-current-temperature");

        currentPrecipitation = Label.New(string.Empty);
        currentPrecipitation.AddCssClass("weather-dim");

        emptyLabel = Label.New("No weather data");
        emptyLabel.AddCssClass("weather-dim");

        var current = Box.New(Orientation.Vertical, 4);
        current.SetValign(Align.Center);
        current.SetSizeRequest(120, -1);
        current.Append(currentIcon);
        current.Append(currentTemperature);
        current.Append(currentPrecipitation);
        current.Append(emptyLabel);
        View.Append(current);

        View.Append(Separator.New(Orientation.Vertical));

        // Right: the hours on top, the days below
        // Homogeneous - when all the hours fit, they spread over the width like the days
        hourly = Box.New(Orientation.Horizontal, 4);
        hourly.SetHomogeneous(true);
        hourly.AddCssClass("weather-hourly");

        // Without the natural width - the days decide how wide the page is, the hours scroll in it
        hourlyScroll = ScrolledWindow.New();
        hourlyScroll.SetPolicy(PolicyType.Automatic, PolicyType.Never);
        hourlyScroll.SetPropagateNaturalHeight(true);
        hourlyScroll.SetChild(hourly);
        HorizontalScroll.AddMouseWheel(hourlyScroll);

        // Fills the page also when the tabs make the popover wider
        daily = Box.New(Orientation.Horizontal, 8);
        daily.SetHomogeneous(true);
        daily.Hexpand = true;
        daily.AddCssClass("weather-daily");

        var forecasts = Box.New(Orientation.Vertical, 12);
        forecasts.Hexpand = true;
        forecasts.Append(hourlyScroll);
        forecasts.Append(Separator.New(Orientation.Horizontal));
        forecasts.Append(daily);
        View.Append(forecasts);

        Forecast.OnChange += OnForecastChanged;
        UserSettings.General.TimeFormat.Changed += OnTimeFormatChanged;
        UserSettings.General.DateFormat.Changed += OnDateFormatChanged;
        Update();
    }

    private void OnForecastChanged(WeatherForecast _) => Update();

    private void OnTimeFormatChanged(TimeFormat _) => Update();

    private void OnDateFormatChanged(string _) => Update();

    private void Update()
    {
        var current = Forecast.Current.Value;

        // No response yet
        emptyLabel.Visible = current is null;
        currentIcon.SetLabel(current?.Icon ?? Icons.Weather.Other);
        currentTemperature.SetLabel(current is null ? string.Empty : FormatTemperature(current));
        currentPrecipitation.SetLabel(current is null ? string.Empty : FormatPrecipitation(current));

        var hourFormat =
            UserSettings.General.TimeFormat.Value == TimeFormat.H12 ? "h tt" : "HH:mm";
        FillRow(
            hourly,
            Forecast.Hourly.Value.Where((_, i) => i % HourStep == 0),
            data => CreateCell(data, [data.Time.ToString(hourFormat, CultureInfo.InvariantCulture)])
        );
        hourlyScroll.Visible = hourly.GetFirstChild() is not null;

        // The culture follows the language from the settings - see UserSettings.ApplyCulture
        var dateFormat = UserSettings.General.DayMonthFormatString;
        FillRow(
            daily,
            Forecast.Daily.Value,
            data =>
                CreateCell(
                    data,
                    [
                        data.Time.ToString("ddd", CultureInfo.CurrentCulture),
                        data.Time.ToString(dateFormat, CultureInfo.InvariantCulture),
                    ]
                )
        );
    }

    // The rows are small, so they are built again on each change
    private static void FillRow(Box row, IEnumerable<WeatherData> items, Func<WeatherData, Box> createCell)
    {
        while (row.GetFirstChild() is { } child)
            row.Remove(child);

        foreach (var data in items)
            row.Append(createCell(data));

        row.Visible = row.GetFirstChild() is not null;
    }

    /// <summary>The time lines, the icon, the temperature and the chance of precipitation.</summary>
    private static Box CreateCell(WeatherData data, string[] timeLines)
    {
        var cell = Box.New(Orientation.Vertical, 2);
        cell.AddCssClass("weather-cell");

        foreach (var line in timeLines)
        {
            var timeLabel = Label.New(line);
            timeLabel.AddCssClass("weather-dim");
            cell.Append(timeLabel);
        }

        var icon = Label.New(data.Icon);
        icon.AddCssClass("weather-icon");
        cell.Append(icon);

        var temperature = Label.New(FormatTemperature(data));
        temperature.AddCssClass("weather-temperature");
        cell.Append(temperature);

        var precipitation = Label.New(FormatPrecipitation(data));
        precipitation.AddCssClass("weather-dim");
        cell.Append(precipitation);

        return cell;
    }

    private static string FormatTemperature(WeatherData data) =>
        data.Temperature is { } value ? $"{value:0}{data.UnitSymbol}" : "-";

    private static string FormatPrecipitation(WeatherData data) =>
        $"{Icons.Weather.precipitationProbability} {(data.PrecipitationProbability is { } value ? $"{value}%" : "-")}";

    public void Dispose()
    {
        Forecast.OnChange -= OnForecastChanged;
        UserSettings.General.TimeFormat.Changed -= OnTimeFormatChanged;
        UserSettings.General.DateFormat.Changed -= OnDateFormatChanged;
    }
}
