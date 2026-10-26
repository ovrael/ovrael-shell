using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;
using OvraelShell.Services;

namespace OvraelShell.Widgets.Obsidian;

[GObject.Subclass<Gtk.Box>]
public sealed partial class ObsidianBarButton : IWithDisposableService<ObsidianService>
{
    public ObsidianService Service { get; private set; }
    private Button button;
    private ObsidianPopover popover;

    public static ObsidianBarButton New(ObsidianService service)
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

        button = Button.NewWithLabel(Icons.Obsidian.Default);
        button.SetCursor(Cursors.Pointer);
        button.SetTooltipText("Daily note");
        Append(button);
    }

    [MemberNotNull(nameof(Service))]
    public void AddService(ObsidianService service)
    {
        Service = service;
    }

    // Safe to call more than once
    public void RemoveService()
    {
        popover.RemoveService();
    }

    [MemberNotNull(nameof(popover))]
    private void InitPopover(ObsidianService service)
    {
        popover = ObsidianPopover.New(service, button);
        button.OnClicked += (_, _) => popover.Popup();
    }
}
