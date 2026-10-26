using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;
using OvraelShell.Services;

namespace OvraelShell.Widgets.Media;

[GObject.Subclass<Gtk.Box>]
public sealed partial class MediaBarButton : IWithDisposableService<MediaService>
{
    public MediaService Service { get; private set; }
    private Button button;
    private MediaPopover popover;

    public static MediaBarButton New(MediaService service)
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

        button = Button.NewWithLabel(Icons.Media.Default);
        button.SetCursor(Cursors.Pointer);
        button.SetTooltipText("Media");
        Append(button);
    }

    [MemberNotNull(nameof(Service))]
    public void AddService(MediaService service)
    {
        Service = service;
    }

    // Safe to call more than once
    public void RemoveService()
    {
        popover.RemoveService();
    }

    [MemberNotNull(nameof(popover))]
    private void InitPopover(MediaService service)
    {
        popover = MediaPopover.New(service, button);
        button.OnClicked += (_, _) => popover.Popup();
    }
}
