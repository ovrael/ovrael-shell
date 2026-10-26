using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;
using OvraelShell.Services;

namespace OvraelShell.Widgets.Notifications;

[GObject.Subclass<Gtk.Box>]
public sealed partial class NotificationsBarButton : IWithDisposableService<NotificationsService>
{
    public NotificationsService Service { get; private set; }
    private Button button;
    private NotificationsPopover popover;

    public static NotificationsBarButton New(NotificationsService service)
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

        button = Button.NewWithLabel(Icons.Notifications.Default);
        button.SetCursor(Cursors.Pointer);
        button.SetTooltipText("Notifications");
        Append(button);
    }

    [MemberNotNull(nameof(Service))]
    public void AddService(NotificationsService service)
    {
        Service = service;
    }

    // Safe to call more than once
    public void RemoveService()
    {
        popover.RemoveService();
    }

    [MemberNotNull(nameof(popover))]
    private void InitPopover(NotificationsService service)
    {
        popover = NotificationsPopover.New(service, button);
        button.OnClicked += (_, _) => popover.Popup();
    }
}
