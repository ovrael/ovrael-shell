using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.Services;
using OvraelShell.Utils;
using OvraelShell.Widgets.Audio.Elements;

namespace OvraelShell.Widgets.Audio;

[GObject.Subclass<Gtk.Popover>]
public sealed partial class AudioPopover : IWithDisposableService<AudioService>
{
    // Share of the monitor height the whole popover may take
    private const double MaxHeightRatio = 0.45;

    // Content margins (2 x 12) plus .popover contents padding and border (2 x 10 + 2 x 1)
    private const int PopoverChrome = 24 + 22;

    public AudioService Service { get; private set; }

    // Created in Initialize, which runs before AddService
    private Box content;
    private StackSwitcher tabs;

    // Homogeneous (the default), so switching tabs does not resize the popover
    private Stack pages;

    // Need the service, so they are created in AddService
    private AudioDirectionPage? outputPage;
    private AudioDirectionPage? inputPage;

    public static AudioPopover New(AudioService audioService, Button parent)
    {
        var widget = NewWithProperties([]);
        widget.AddService(audioService);
        widget.SetParent(parent);
        return widget;
    }

    [MemberNotNull(nameof(content), nameof(tabs), nameof(pages))]
    partial void Initialize()
    {
        CreateContent();

        SetPosition(PositionType.Top);
        SetAutohide(true);
        SetChild(content);
        AddCssClass("popover");

        OnShow += (_, _) => OnShown();
    }

    [MemberNotNull(nameof(Service))]
    public void AddService(AudioService audioService)
    {
        Service = audioService;

        outputPage = AudioDirectionPage.New(audioService, AudioDirection.Output);
        pages.AddTitled(outputPage, "output", "Output");

        inputPage = AudioDirectionPage.New(audioService, AudioDirection.Input);
        pages.AddTitled(inputPage, "input", "Input");
    }

    public void RemoveService()
    {
        outputPage?.RemoveService();
        inputPage?.RemoveService();
    }

    [MemberNotNull(nameof(content), nameof(tabs), nameof(pages))]
    private void CreateContent()
    {
        content = Box.New(Orientation.Vertical, 8);
        content.SetMarginTop(12);
        content.SetMarginBottom(12);
        content.SetMarginStart(12);
        content.SetMarginEnd(12);

        pages = Stack.New();

        tabs = StackSwitcher.New();
        tabs.SetStack(pages);
        tabs.SetHalign(Align.Center);
        tabs.AddCssClass("audio-tabs");

        content.Append(tabs);
        content.Append(pages);
    }

    private void OnShown()
    {
        outputPage?.ResetHeights();
        inputPage?.ResetHeights();

        UpdateMaxHeight();
    }

    /// <summary>
    /// Limits the pages so the whole popover takes about <see cref="MaxHeightRatio"/> of the
    /// monitor the bar is on. Done on every show - the bar can move between monitors.
    /// </summary>
    private void UpdateMaxHeight()
    {
        var monitorHeight = GetParent() is { } parent ? MonitorSize.HeightOf(parent) : 0;

        if (monitorHeight <= 0)
            return;

        // Everything above the pages: the tabs and the gap below them
        tabs.Measure(Orientation.Vertical, -1, out _, out var tabsHeight, out _, out _);

        var reserved = tabsHeight + content.GetSpacing() + PopoverChrome;
        var maxPageHeight = (int)(monitorHeight * MaxHeightRatio) - reserved;

        outputPage?.UpdateMaxHeight(maxPageHeight);
        inputPage?.UpdateMaxHeight(maxPageHeight);
    }
}
