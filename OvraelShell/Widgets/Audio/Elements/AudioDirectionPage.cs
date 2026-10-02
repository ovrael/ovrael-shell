using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.Models.Audio.PipeWire;
using OvraelShell.Services;

namespace OvraelShell.Widgets.Audio.Elements;

/// <summary>
/// One tab of the audio popover - the devices of a direction (outputs or inputs) on top,
/// the streams playing to them (or recording from them) below.
/// </summary>
[GObject.Subclass<Gtk.Box>]
public sealed partial class AudioDirectionPage : IWithDisposableService<AudioService>
{
    // Space the streams list keeps even on a small monitor
    private const int MinStreamsHeight = 100;

    public AudioService Service { get; private set; }

    // Created in Initialize, which runs before AddService
    private Label devicesTitle;
    private ScrolledWindow devicesScroll;
    private Label streamsTitle;
    private ScrolledWindow streamsScroll;

    // Need the service and the direction, so they are created in AddService
    private AudioDirection direction;
    private PipeWireNodeList? deviceList;
    private PipeWireNodeList? streamList;

    public static AudioDirectionPage New(AudioService audioService, AudioDirection direction)
    {
        var widget = NewWithProperties([]);
        widget.direction = direction;
        widget.AddService(audioService);
        return widget;
    }

    [MemberNotNull(
        nameof(devicesTitle),
        nameof(devicesScroll),
        nameof(streamsTitle),
        nameof(streamsScroll)
    )]
    partial void Initialize()
    {
        SetOrientation(Orientation.Vertical);
        SetSpacing(8);

        devicesTitle = Label.New("Devices");
        Append(devicesTitle);

        devicesScroll = CreateScroll();
        Append(devicesScroll);

        streamsTitle = Label.New("Streams");
        Append(streamsTitle);

        streamsScroll = CreateScroll();
        Append(streamsScroll);
    }

    // Grows with the list up to the max height, then scrolls
    private static ScrolledWindow CreateScroll()
    {
        var scroll = ScrolledWindow.New();
        scroll.SetPolicy(PolicyType.Never, PolicyType.Automatic);
        scroll.SetPropagateNaturalHeight(true);
        return scroll;
    }

    [MemberNotNull(nameof(Service))]
    public void AddService(AudioService audioService)
    {
        Service = audioService;

        var isOutput = direction == AudioDirection.Output;

        deviceList = new PipeWireNodeList(
            audioService,
            isOutput ? PipeWireMediaTypes.AudioSink : PipeWireMediaTypes.AudioSource
        );
        deviceList.RowsChanging += HoldDevicesHeight;
        devicesScroll.SetChild(deviceList.View);

        streamList = new PipeWireNodeList(
            audioService,
            isOutput ? PipeWireMediaTypes.StreamOutputAudio : PipeWireMediaTypes.StreamInputAudio
        );
        streamList.RowsChanging += HoldStreamsHeight;
        streamsScroll.SetChild(streamList.View);
    }

    public void RemoveService()
    {
        // Before Dispose - it empties the lists, which would resize the popover
        if (deviceList is not null)
        {
            deviceList.RowsChanging -= HoldDevicesHeight;
            deviceList.Dispose();
        }

        if (streamList is not null)
        {
            streamList.RowsChanging -= HoldStreamsHeight;
            streamList.Dispose();
        }
    }

    /// <summary>Lets the lists fit their rows anew - called on every show of the popover.</summary>
    public void ResetHeights()
    {
        // Before the max, which may not go below the min
        devicesScroll.SetMinContentHeight(-1);
        streamsScroll.SetMinContentHeight(-1);
    }

    /// <summary>
    /// Limits the page to <paramref name="maxHeight"/>. Devices are few, so they take what
    /// they need and the streams scroll in the rest.
    /// </summary>
    public void UpdateMaxHeight(int maxHeight)
    {
        devicesTitle.Measure(Orientation.Vertical, -1, out _, out var devicesTitleHeight, out _, out _);
        streamsTitle.Measure(Orientation.Vertical, -1, out _, out var streamsTitleHeight, out _, out _);

        var reserved = devicesTitleHeight + streamsTitleHeight + 3 * GetSpacing();
        var maxListsHeight = Math.Max(MinStreamsHeight, maxHeight - reserved);

        // Leave the streams at least their minimum when there are many devices
        var maxDevicesHeight = Math.Max(MinStreamsHeight, maxListsHeight - MinStreamsHeight);
        devicesScroll.SetMaxContentHeight(maxDevicesHeight);

        devicesScroll.Measure(Orientation.Vertical, -1, out _, out var devicesHeight, out _, out _);
        devicesHeight = Math.Min(devicesHeight, maxDevicesHeight);

        streamsScroll.SetMaxContentHeight(Math.Max(MinStreamsHeight, maxListsHeight - devicesHeight));
    }

    private void HoldDevicesHeight() => HoldListHeight(devicesScroll);

    private void HoldStreamsHeight() => HoldListHeight(streamsScroll);

    /// <summary>
    /// Keeps a list from shrinking while the popover is open - every size change makes GTK
    /// present the popup again, which flickers on Hyprland (see NetworkPopover.HoldListHeight).
    /// Streams come and go often: an application opens one per sound it plays.
    /// Called before the change is laid out, so the current height is the one to keep.
    /// </summary>
    private void HoldListHeight(ScrolledWindow scroll)
    {
        // The popover, not the page - the hidden tab still sizes the stack
        if (GetAncestor(Popover.GetGType()) is not Popover { Visible: true })
            return;

        var height = scroll.GetHeight();
        var maxHeight = scroll.GetMaxContentHeight();

        if (maxHeight >= 0)
            height = Math.Min(height, maxHeight);

        if (height > scroll.GetMinContentHeight())
            scroll.SetMinContentHeight(height);
    }
}
