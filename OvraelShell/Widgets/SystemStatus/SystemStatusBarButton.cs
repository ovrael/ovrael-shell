using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;
using OvraelShell.Services;

namespace OvraelShell.Widgets.SystemStatus;

[GObject.Subclass<Gtk.Box>]
public sealed partial class SystemStatusBarButton : IWithDisposableService<SystemStatusService>
{
    public SystemStatusService Service { get; private set; }
    private Button button;
    private SystemStatusPopover popover;

    public static SystemStatusBarButton New(SystemStatusService service)
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

        button = Button.NewWithLabel(Icons.SystemStatus.Cpu);
        button.SetCursor(Cursors.Pointer);
        button.SetTooltipText("System status");
        Append(button);
    }

    [MemberNotNull(nameof(Service))]
    public void AddService(SystemStatusService service)
    {
        Service = service;
    }

    // Safe to call more than once
    public void RemoveService()
    {
        popover.RemoveService();
    }

    [MemberNotNull(nameof(popover))]
    private void InitPopover(SystemStatusService service)
    {
        popover = SystemStatusPopover.New(service, button);
        button.OnClicked += (_, _) => popover.Popup();
    }
}
