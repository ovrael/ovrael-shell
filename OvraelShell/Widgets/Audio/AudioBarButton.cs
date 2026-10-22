using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;
using OvraelShell.Models.Audio.PipeWire;
using OvraelShell.Services;

namespace OvraelShell.Widgets.Audio;

[GObject.Subclass<Gtk.Box>]
public sealed partial class AudioBarButton : IWithDisposableService<AudioService>
{
    private Button button;
    private AudioPopover popover;
    public AudioService Service { get; private set; }

    // The default output - followed for its volume and mute, see SetOutput
    private PipeWireNode? output;

    public static AudioBarButton New(AudioService audio)
    {
        var widget = NewWithProperties([]);
        widget.AddService(audio);
        widget.InitPopover(audio);
        return widget;
    }

    [MemberNotNull(nameof(button))]
    partial void Initialize()
    {
        SetOrientation(Orientation.Horizontal);

        button = Button.NewWithLabel(Icons.Audio.VolumeMedium);
        button.SetCursor(Cursors.Pointer);
        Append(button);
    }

    [MemberNotNull(nameof(Service))]
    public void AddService(AudioService audio)
    {
        Service = audio;

        Service.DefaultOutput.Changed += SetOutput;
        SetOutput(Service.DefaultOutput.Value);
    }

    // Safe to call more than once
    public void RemoveService()
    {
        Service.DefaultOutput.Changed -= SetOutput;

        // The node belongs to the service too
        SetOutput(null);
        popover.RemoveService();
    }

    [MemberNotNull(nameof(popover))]
    private void InitPopover(AudioService audio)
    {
        popover = AudioPopover.New(audio, button);
        button.OnClicked += (_, _) => popover.Popup();
    }

    // OnChange, not Volume.Changed - the first volume can equal the default (0),
    // and HasVolume still changes then
    private void SetOutput(PipeWireNode? node)
    {
        output?.OnChange -= OnOutputChanged;
        output = node;
        output?.OnChange += OnOutputChanged;

        UpdateLabel();
    }

    private void OnOutputChanged(PipeWireNode _) => UpdateLabel();

    private void UpdateLabel()
    {
        // No default output, or its volume is not known yet
        if (output is not { HasVolume: true })
        {
            button.SetLabel(Icons.Audio.VolumeOff);
            button.SetTooltipText(output is null ? "No output" : output.DisplayName);
            return;
        }

        // The same scale as the sliders in the popover
        var percent = (uint)Math.Round(output.Volume.Value * 100);

        button.SetLabel(AudioIcons.Volume(percent, output.IsMuted.Value));
        button.SetTooltipText(
            $"{output.DisplayName}: {(output.IsMuted.Value ? "muted" : $"{percent}%")}"
        );
    }
}
