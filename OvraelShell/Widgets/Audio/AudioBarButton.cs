using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;
using OvraelShell.Services;

namespace OvraelShell.Widgets.Audio;

[GObject.Subclass<Gtk.Box>]
public sealed partial class AudioBarButton : IWithDisposableService<AudioService>
{
    private Button button;
    private AudioPopover popover;
    public AudioService Service { get; set; }

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
    }

    public void RemoveService()
    {
        popover.RemoveService();
    }

    [MemberNotNull(nameof(popover))]
    private void InitPopover(AudioService audio)
    {
        popover = AudioPopover.New(audio, button);
        button.OnClicked += (_, _) => popover.Popup();
    }

    private void OnVolumeChanged(byte _) => UpdateLabel();

    private void UpdateLabel()
    {
        if (Service.ActiveDevice.Value is null)
            return;

        var isMuted = Service.ActiveDevice.Value.IsMuted;
        var icon = AudioIcons.Volume(Service.ActiveDevice.Value.Volume, isMuted);

        button.SetLabel(icon);
    }
}
