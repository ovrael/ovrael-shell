using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;
using OvraelShell.Models.Network;
using OvraelShell.Services;
using OvraelShell.Utils;

namespace OvraelShell.Widgets.Network;

[GObject.Subclass<Gtk.Box>]
public sealed partial class WeatherBarButton : IWithDisposableService<WeatherService>
{
    public WeatherService Service { get; private set; }
    private Button button;
    private WeatherPopover popover;
    private WeatherLocation? activeLocation;

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

        button = Button.NewWithLabel(Icons.Network.EthernetDisconnected);
        button.SetCursor(Cursors.Pointer);
        Append(button);
    }

    [MemberNotNull(nameof(Service))]
    public void AddService(WeatherService network)
    {
        Service = network;
    }

    public void RemoveService()
    {
        popover.RemoveService();
    }

    private void OnWeatherChange<T>(T _) => UpdateLabel();

    [MemberNotNull(nameof(popover))]
    private void InitPopover(WeatherService service)
    {
        popover = WeatherPopover.New(service, button);
        button.OnClicked += (_, _) => popover.Popup();
    }

    private void UpdateLabel()
    {
        button.SetLabel("Katowice X");
    }
}
