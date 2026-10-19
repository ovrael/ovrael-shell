using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.Services;
using OvraelShell.Utils;
using OvraelShell.Widgets.Network.Elements;

namespace OvraelShell.Widgets.Network;

[GObject.Subclass<Gtk.Popover>]
public sealed partial class WeatherPopover : IWithDisposableService<WeatherService>
{
    public WeatherService Service { get; private set; }

    private Box content;

    public static WeatherPopover New(WeatherService service, Button parent)
    {
        var widget = NewWithProperties([]);
        widget.AddService(service);
        widget.SetParent(parent);
        return widget;
    }

    [MemberNotNull(nameof(content))]
    partial void Initialize()
    {
        CreateContent();

        SetPosition(PositionType.Top);
        SetAutohide(true);
        SetChild(content);
        AddCssClass("popover");
    }

    [MemberNotNull(nameof(Service))]
    public void AddService(WeatherService networkService)
    {
        Service = networkService;
    }

    public void RemoveService() { }

    [MemberNotNull(nameof(content))]
    private void CreateContent()
    {
        content = Box.New(Orientation.Vertical, 8);
        content.SetMarginTop(12);
        content.SetMarginBottom(12);
        content.SetMarginStart(12);
        content.SetMarginEnd(12);
    }
}
