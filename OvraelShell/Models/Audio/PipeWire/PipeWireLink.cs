namespace OvraelShell.Models.Audio.PipeWire;

/// <summary>
/// Connection between an output port of one node and an input port of another.
/// A stereo stream playing on a sink has two links - one per channel.
/// </summary>
public class PipeWireLink
{
    /*
        object.serial = 999
        factory.id = 21
        client.id = 32
        link.output.port = 88
        link.input.port = 61
        link.output.node = 87
        link.input.node = 55
    */

    public uint GlobalId { get; set; }
    public string ObjectSerial { get; set; }

    // Global ids of the nodes and ports on both ends
    public uint OutputNode { get; set; }
    public uint OutputPort { get; set; }
    public uint InputNode { get; set; }
    public uint InputPort { get; set; }

    public PipeWireLink(PipeWireGlobal global)
    {
        GlobalId = global.Id;
        ObjectSerial = global.Prop("object.serial") ?? string.Empty;

        // Always set by PipeWire - 0 is the core, so it never matches an audio node
        OutputNode = global.PropId("link.output.node") ?? 0;
        OutputPort = global.PropId("link.output.port") ?? 0;
        InputNode = global.PropId("link.input.node") ?? 0;
        InputPort = global.PropId("link.input.port") ?? 0;
    }
}
