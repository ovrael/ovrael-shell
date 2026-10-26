using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.Services;
using OvraelShell.Widgets.Shared;

namespace OvraelShell.Widgets.Notifications;

[GObject.Subclass<Gtk.Popover>]
public sealed partial class NotificationsPopover : IWithDisposableService<NotificationsService>
{
    public NotificationsService Service { get; private set; }

    private Box content;

    public static NotificationsPopover New(NotificationsService service, Button parent)
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
    public void AddService(NotificationsService service)
    {
        Service = service;
    }

    // Safe to call more than once
    public void RemoveService() { }

    [MemberNotNull(nameof(content))]
    private void CreateContent()
    {
        content = PlaceholderPage.Create(
            "Notifications",
            "No notification daemon runs now - the shell must own org.freedesktop.Notifications (Gio.BusOwnName). Methods: Notify, CloseNotification, GetCapabilities, GetServerInformation; signals: NotificationClosed, ActionInvoked. History with a limit, saved in $XDG_STATE_HOME; popups in a separate layer-shell window. Do not disturb mode. If another daemon owns the name, show that the history is not available."
        );
    }
}
