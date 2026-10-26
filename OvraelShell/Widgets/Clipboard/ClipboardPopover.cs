using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.Services;
using OvraelShell.Widgets.Shared;

namespace OvraelShell.Widgets.Clipboard;

[GObject.Subclass<Gtk.Popover>]
public sealed partial class ClipboardPopover : IWithDisposableService<ClipboardService>
{
    public ClipboardService Service { get; private set; }

    private Box content;

    public static ClipboardPopover New(ClipboardService service, Button parent)
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
    public void AddService(ClipboardService service)
    {
        Service = service;
    }

    // Safe to call more than once
    public void RemoveService() { }

    [MemberNotNull(nameof(content))]
    private void CreateContent()
    {
        content = PlaceholderPage.Create(
            "Clipboard history",
            "Source: cliphist (wl-paste --watch cliphist store) - list with 'cliphist list', copy again with 'cliphist decode <id> | wl-copy'. Without an external daemon: the ext-data-control-v1 Wayland protocol (more work). Do not keep entries of password managers (x-kde-passwordManagerHint: secret). Image entries: a preview from 'cliphist decode' as a Gdk.Texture."
        );
    }
}
