namespace OvraelShell.Models.Audio.PipeWire;

public sealed record PipeWireGlobal(
    uint Id,
    string Type,
    uint Version,
    IReadOnlyDictionary<string, string> Props
)
{
    public string? Prop(string key) => Props.TryGetValue(key, out var value) ? value : null;

    /// <summary>A prop holding the global id of another object, like device.id.</summary>
    public uint? PropId(string key) => uint.TryParse(Prop(key), out var id) ? id : null;
}
