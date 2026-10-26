using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.Services;
using OvraelShell.Widgets.Shared;

namespace OvraelShell.Widgets.Media;

[GObject.Subclass<Gtk.Popover>]
public sealed partial class MediaPopover : IWithDisposableService<MediaService>
{
    public MediaService Service { get; private set; }

    private Box content;

    public static MediaPopover New(MediaService service, Button parent)
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
    public void AddService(MediaService service)
    {
        Service = service;
    }

    // Safe to call more than once
    public void RemoveService() { }

    [MemberNotNull(nameof(content))]
    private void CreateContent()
    {
        content = PlaceholderPage.Create(
            "Media",
            "MPRIS on the session bus: the org.mpris.MediaPlayer2.* names, follow NameOwnerChanged. Per player: PlaybackStatus, Metadata (title, artist, mpris:artUrl), CanGoNext/CanGoPrevious. Position has no change signal - poll it only while the popover is open. Actions: PlayPause, Next, Previous, SetPosition. Skip playerctld - it only repeats the other players."
        );
    }
}
