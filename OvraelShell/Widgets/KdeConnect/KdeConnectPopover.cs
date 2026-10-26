using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.Services;
using OvraelShell.Widgets.Shared;

namespace OvraelShell.Widgets.KdeConnect;

[GObject.Subclass<Gtk.Popover>]
public sealed partial class KdeConnectPopover : IWithDisposableService<KdeConnectService>
{
    public KdeConnectService Service { get; private set; }

    private Box content;

    public static KdeConnectPopover New(KdeConnectService service, Button parent)
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
    public void AddService(KdeConnectService service)
    {
        Service = service;
    }

    // Safe to call more than once
    public void RemoveService() { }

    [MemberNotNull(nameof(content))]
    private void CreateContent()
    {
        content = PlaceholderPage.Create(
            "KDE Connect",
            "kdeconnectd on the session bus: org.kde.kdeconnect, /modules/kdeconnect - devices(onlyReachable, onlyPaired). Per device /modules/kdeconnect/devices/<id>: name, isReachable, battery (charge, isCharging). Actions: ping, find the phone (ring), share a file or a URL, send the clipboard."
        );
    }
}
