using OvraelShell.Interfaces;

namespace OvraelShell.Models.Audio.PipeWire;

[GObject.Subclass<PipeWireBase>]
public sealed partial class PipeWireNode : IReactivable<PipeWireNode>
{
    /*
        object.serial = 8023
        object.path = alsa:acp:Wireless:2:capture
        factory.id = 19
        client.id = 32
        device.id = 64
        priority.session = 2100
        priority.driver = 2100
        node.description = HyperX Cloud Alpha Wireless Mono
        node.name = alsa_input.usb-HP__Inc_HyperX_Cloud_Alpha_Wireless_00000001-00.mono-fallback
        node.nick = HyperX Cloud Alpha Wireless
        media.class = Audio/Source

        object.serial = 15214
        factory.id = 9
        client.id = 70
        application.name = PipeWire ALSA [psysonic-bin]
        node.description = ALSA Playback [psysonic-bin]
        node.name = alsa_playback.psysonic-bin
        media.class = Stream/Output/Audio
        media.type = Audio
        media.category = Playback
    */

    // Global id of the PipeWireDevice this node belongs to - streams have none
    public uint? DeviceId { get; private set; }
    public string? MediaType { get; private set; }
    public string? PrioritySession { get; private set; }
    public string? PriorityDriver { get; private set; }

    /// <summary>
    /// card.profile.device from the full props - which route of the device the node plays
    /// through. Null for nodes without a device and until the props arrive.
    /// </summary>
    public int? CardProfileDevice { get; private set; }

    /// <summary>Linear volume of each channel, as PipeWire reports it. Empty until known.</summary>
    public float[] ChannelVolumes { get; private set; } = [];

    /// <summary>Volume of the loudest channel on the scale of sliders - cubic, like wpctl and pavucontrol.</summary>
    public ReactiveProperty<double> Volume { get; } = new();

    public ReactiveProperty<bool> IsMuted { get; } = new();

    /// <summary>The volume is known - set once the first Props param arrives.</summary>
    public bool HasVolume => ChannelVolumes.Length > 0;

    public event Action<PipeWireNode>? OnChange;

    internal void UpdateCardProfileDevice(int? cardProfileDevice) =>
        CardProfileDevice = cardProfileDevice;

    /// <summary>From the Props param - sets only what it contains.</summary>
    internal void UpdateVolume(float[]? channelVolumes, bool? mute)
    {
        if (channelVolumes is { Length: > 0 })
        {
            ChannelVolumes = channelVolumes;
            Volume.Set(Math.Cbrt(channelVolumes.Max()));
        }

        if (mute is not null)
            IsMuted.Set(mute.Value);

        NotifyChanged();
    }

    /// <summary>Raised by AudioService when something about the node changes, like its links.</summary>
    internal void NotifyChanged() => OnChange?.Invoke(this);

    public static PipeWireNode Create(PipeWireGlobal global)
    {
        var node = NewWithProperties([]);
        node.Fill(global, "node");

        node.DeviceId = global.PropId("device.id");
        node.MediaType = global.Prop("media.type");
        node.PrioritySession = global.Prop("priority.session");
        node.PriorityDriver = global.Prop("priority.driver");

        return node;
    }

    public override string ToString() =>
        $"PipeWireNode {{ GlobalId = {GlobalId}, MediaClass = {MediaClass}, Name = {Name}, "
        + $"Description = {Description}, DeviceId = {DeviceId} }}";
}
