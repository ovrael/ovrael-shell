using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;
using OvraelShell.Services;

namespace OvraelShell.Widgets.Trash;

[GObject.Subclass<Gtk.Box>]
public sealed partial class TrashBarButton : IWithDisposableService<TrashService>
{
    public TrashService Service { get; private set; }
    private Button button;
    private TrashPopover popover;

    public static TrashBarButton New(TrashService service)
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

        button = Button.NewWithLabel(Icons.Trash.Empty);
        button.SetCursor(Cursors.Pointer);
        button.SetTooltipText("Trash");
        Append(button);
    }

    [MemberNotNull(nameof(Service))]
    public void AddService(TrashService service)
    {
        Service = service;
    }

    // Safe to call more than once
    public void RemoveService()
    {
        popover.RemoveService();
    }

    [MemberNotNull(nameof(popover))]
    private void InitPopover(TrashService service)
    {
        popover = TrashPopover.New(service, button);
        button.OnClicked += (_, _) => popover.Popup();
    }
}
