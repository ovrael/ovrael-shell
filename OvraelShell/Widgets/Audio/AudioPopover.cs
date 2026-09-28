using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.Services;
using OvraelShell.Utils;
using OvraelShell.Widgets.Elements.Network;
using ZwlrLayerShell;

namespace OvraelShell.Widgets.Audio;

[GObject.Subclass<Gtk.Popover>]
public sealed partial class AudioPopover : IWithDisposableService<AudioService>
{
    // Share of the monitor height the whole popover may take
    private const double MaxHeightRatio = 0.45;

    // Content margins (2 x 12) plus .popover contents padding and border (2 x 10 + 2 x 1)
    private const int PopoverChrome = 24 + 22;

    public AudioService Service { get; private set; }

    // Created in Initialize, which runs before AddNetworkService
    private Box content;

    public static AudioPopover New(AudioService audioService, Button parent)
    {
        var widget = NewWithProperties([]);
        widget.AddService(audioService);
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

        OnShow += (_, _) => OnShown();
        OnHide += (_, _) => OnHidden();
    }

    [MemberNotNull(nameof(Service))]
    public void AddService(AudioService audioService)
    {
        Service = audioService;
    }

    public void RemoveService()
    {
        
    }

    [MemberNotNull(nameof(content))]
    private void CreateContent()
    {
        content = Box.New(Orientation.Vertical, 8);
        content.SetMarginTop(12);
        content.SetMarginBottom(12);
        content.SetMarginStart(12);
        content.SetMarginEnd(12);
    }

    private void OnShown() { }

    // Hand the keyboard back when closing. With OnDemand the compositor keeps
    // focus on the bar until another window is clicked, so typing elsewhere
    // would not work after the popover closes.
    private void OnHidden() { }
}
