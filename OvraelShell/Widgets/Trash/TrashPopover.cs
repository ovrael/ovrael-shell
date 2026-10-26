using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.Services;
using OvraelShell.Widgets.Shared;

namespace OvraelShell.Widgets.Trash;

[GObject.Subclass<Gtk.Popover>]
public sealed partial class TrashPopover : IWithDisposableService<TrashService>
{
    public TrashService Service { get; private set; }

    private Box content;

    public static TrashPopover New(TrashService service, Button parent)
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
    public void AddService(TrashService service)
    {
        Service = service;
    }

    // Safe to call more than once
    public void RemoveService() { }

    [MemberNotNull(nameof(content))]
    private void CreateContent()
    {
        content = PlaceholderPage.Create(
            "Trash",
            "Gio.File trash:/// - enumerate with trash::orig-path and trash::deletion-date (also covers .Trash-1000 on other mounts). Count on the bar from trash::item-count, changes from a FileMonitor on trash:///. Restore one item, or all items from one original folder (move the item to trash::orig-path, like gio trash --restore). Empty the trash only after a confirmation. Enumerate on Task.Run - large folders take time."
        );
    }
}
