namespace OvraelShell.Models.Audio.PipeWire;

[GObject.Subclass<PipeWireBase>]
public sealed partial class PipeWireDevice
{
    /*
        object.serial = 8014
        factory.id = 15
        client.id = 41
        device.api = alsa
        device.description = GA104 High Definition Audio Controller
        device.name = alsa_card.pci-0000_01_00.1
        device.nick = HDA NVidia
        media.class = Audio/Device
        object.path = alsa:acp:NVidia
    */

    /// <summary>Indexes of the active routes by card.profile.device of the node using them.</summary>
    internal Dictionary<int, int> RouteIndexByCardDevice { get; } = new();

    public static PipeWireDevice Create(PipeWireGlobal global)
    {
        var device = NewWithProperties([]);
        device.Fill(global, "device");

        return device;
    }

    public override string ToString() =>
        $"PipeWireDevice {{ GlobalId = {GlobalId}, Api = {Api}, Name = {Name}, "
        + $"Description = {Description} }}";
}
