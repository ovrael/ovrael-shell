using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;
using OvraelShell.Models.Weather;
using OvraelShell.Services;
using OvraelShell.Utils;
using OvraelShell.Utils.Settings;

namespace OvraelShell.Widgets.Weather;

[GObject.Subclass<Gtk.Box>]
public sealed partial class WeatherBarButton : IWithDisposableService<WeatherService>
{
    public WeatherService Service { get; private set; }
    private Button button;
    private WeatherPopover popover;

    // The main location of the service - followed for its weather
    private WeatherForecast? mainForecast;

    public static WeatherBarButton New(WeatherService service)
    {
        var widget = NewWithProperties([]);
        widget.AddService(service);
        widget.InitPopover(service);
        return widget;
    }

    [MemberNotNull(nameof(button))]
    partial void Initialize()
    {
        SetOrientation(Orientation.Horizontal);

        button = Button.NewWithLabel(Icons.Weather.Other);
        button.SetCursor(Cursors.Pointer);
        Append(button);
    }

    [MemberNotNull(nameof(Service))]
    public void AddService(WeatherService service)
    {
        Service = service;

        // The service sets the main location in its constructor - before this subscription
        Service.MainLocation.Changed += SetMainForecast;
        UserSettings.Weather.ShowLocationName.Changed += OnShowLocationNameChanged;
        SetMainForecast(Service.MainLocation.Value);
    }

    // Safe to call more than once
    public void RemoveService()
    {
        Service.MainLocation.Changed -= SetMainForecast;
        UserSettings.Weather.ShowLocationName.Changed -= OnShowLocationNameChanged;

        // The forecast belongs to the service too
        SetMainForecast(null);
        popover.RemoveService();
    }

    // MainLocation changes only when another location is the main one -
    // the weather changes inside the forecast, so its OnChange is followed
    private void SetMainForecast(WeatherForecast? forecast)
    {
        mainForecast?.OnChange -= OnForecastChanged;
        mainForecast = forecast;
        mainForecast?.OnChange += OnForecastChanged;

        UpdateLabel();
    }

    private void OnForecastChanged(WeatherForecast _) => UpdateLabel();

    private void OnShowLocationNameChanged(bool _) => UpdateLabel();

    [MemberNotNull(nameof(popover))]
    private void InitPopover(WeatherService service)
    {
        popover = WeatherPopover.New(service, button);
        button.OnClicked += (_, _) => popover.Popup();
    }

    private void UpdateLabel()
    {
        // No location in the settings, or no response yet
        if (mainForecast?.Current.Value is not { } current)
        {
            button.SetLabel(Icons.Weather.Other);
            return;
        }

        var temperature = current.Temperature is { } value ? $"{value:0}{current.UnitSymbol}" : "-";
        var name =
            UserSettings.Weather.ShowLocationName.Value && mainForecast.Name.Value.Length > 0
                ? $" {mainForecast.Name.Value}"
                : "";

        button.SetLabel($"{current.Icon} {temperature}{name}");
    }
}
