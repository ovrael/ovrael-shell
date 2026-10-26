using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;
using OvraelShell.Services;
using OvraelShell.Widgets.Shared;

namespace OvraelShell.Widgets.Tray;

[GObject.Subclass<Gtk.Popover>]
public sealed partial class TrayPopover : IWithDisposableService<TrayService>
{
    public TrayService Service { get; private set; }

    private Box content;

    public static TrayPopover New(TrayService service, Button parent)
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
    public void AddService(TrayService service)
    {
        Service = service;
    }

    // Safe to call more than once
    public void RemoveService() { }

    [MemberNotNull(nameof(content))]
    private void CreateContent()
    {
        content = Box.New(Orientation.Vertical, 8);
        content.SetMarginTop(12);
        content.SetMarginBottom(12);
        content.SetMarginStart(12);
        content.SetMarginEnd(12);

        // Homogeneous (the default), so switching tabs does not resize the popover
        var pages = Stack.New();
        pages.AddTitled(
            PlaceholderPage.Create(
                "Apps",
                "StatusNotifierItem: the shell is the org.kde.StatusNotifierWatcher and the host (none runs now, so the Steam icon is not shown anywhere). Menus of the items through com.canonical.dbusmenu."
            ),
            "apps",
            "Apps"
        );
        pages.AddTitled(
            PlaceholderPage.Create(
                "Drives",
                "UDisks2 on the system bus: removable block devices (pendrives, cards). Actions: Filesystem.Mount, Filesystem.Unmount, Drive.PowerOff / Eject."
            ),
            "drives",
            "Drives"
        );
        pages.AddTitled(
            PlaceholderPage.Create(
                "Services",
                "List from the settings: name, how to check the state, how to start and stop it. systemd user units through org.freedesktop.systemd1, other programs by process (Steam)."
            ),
            "services",
            "Services"
        );

        var tabs = StackSwitcher.New();
        tabs.SetStack(pages);
        tabs.SetHalign(Align.Center);
        tabs.AddCssClass("tray-tabs");

        // The tab buttons are created by the StackSwitcher - they take the cursor from it
        tabs.SetCursor(Cursors.Pointer);

        content.Append(tabs);
        content.Append(pages);
    }
}
