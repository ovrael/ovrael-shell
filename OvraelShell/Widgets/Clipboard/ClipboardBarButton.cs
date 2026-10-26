using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;
using OvraelShell.Services;

namespace OvraelShell.Widgets.Clipboard;

[GObject.Subclass<Gtk.Box>]
public sealed partial class ClipboardBarButton : IWithDisposableService<ClipboardService>
{
    public ClipboardService Service { get; private set; }
    private Button button;
    private ClipboardPopover popover;

    public static ClipboardBarButton New(ClipboardService service)
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

        button = Button.NewWithLabel(Icons.Clipboard.Default);
        button.SetCursor(Cursors.Pointer);
        button.SetTooltipText("Clipboard history");
        Append(button);
    }

    [MemberNotNull(nameof(Service))]
    public void AddService(ClipboardService service)
    {
        Service = service;
    }

    // Safe to call more than once
    public void RemoveService()
    {
        popover.RemoveService();
    }

    [MemberNotNull(nameof(popover))]
    private void InitPopover(ClipboardService service)
    {
        popover = ClipboardPopover.New(service, button);
        button.OnClicked += (_, _) => popover.Popup();
    }
}
