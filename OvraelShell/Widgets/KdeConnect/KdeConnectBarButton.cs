using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;
using OvraelShell.Services;

namespace OvraelShell.Widgets.KdeConnect;

[GObject.Subclass<Gtk.Box>]
public sealed partial class KdeConnectBarButton : IWithDisposableService<KdeConnectService>
{
    public KdeConnectService Service { get; private set; }
    private Button button;
    private KdeConnectPopover popover;

    public static KdeConnectBarButton New(KdeConnectService service)
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

        button = Button.NewWithLabel(Icons.KdeConnect.Disconnected);
        button.SetCursor(Cursors.Pointer);
        button.SetTooltipText("KDE Connect");
        Append(button);
    }

    [MemberNotNull(nameof(Service))]
    public void AddService(KdeConnectService service)
    {
        Service = service;
    }

    // Safe to call more than once
    public void RemoveService()
    {
        popover.RemoveService();
    }

    [MemberNotNull(nameof(popover))]
    private void InitPopover(KdeConnectService service)
    {
        popover = KdeConnectPopover.New(service, button);
        button.OnClicked += (_, _) => popover.Popup();
    }
}
