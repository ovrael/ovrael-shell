using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;
using OvraelShell.Services;

namespace OvraelShell.Widgets.Tray;

[GObject.Subclass<Gtk.Box>]
public sealed partial class TrayBarButton : IWithDisposableService<TrayService>
{
    public TrayService Service { get; private set; }
    private Button button;
    private TrayPopover popover;

    public static TrayBarButton New(TrayService service)
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

        button = Button.NewWithLabel(Icons.Tray.Default);
        button.SetCursor(Cursors.Pointer);
        button.SetTooltipText("Tray");
        Append(button);
    }

    [MemberNotNull(nameof(Service))]
    public void AddService(TrayService service)
    {
        Service = service;
    }

    // Safe to call more than once
    public void RemoveService()
    {
        popover.RemoveService();
    }

    [MemberNotNull(nameof(popover))]
    private void InitPopover(TrayService service)
    {
        popover = TrayPopover.New(service, button);
        button.OnClicked += (_, _) => popover.Popup();
    }
}
