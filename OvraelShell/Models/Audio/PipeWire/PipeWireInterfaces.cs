namespace OvraelShell.Models.Audio.PipeWire;

public static class PipeWireInterfaces
{
    // Core, Module, SecurityContext, Profiler, Factory, Node, Metadata, Client, Port, Device, Link
    public const string Prefix = "PipeWire:Interface:";
    public const string Core = $"{Prefix}Core";
    public const string Module = $"{Prefix}Module";
    public const string SecurityContext = $"{Prefix}SecurityContext";
    public const string Profiler = $"{Prefix}Profiler";
    public const string Factory = $"{Prefix}Factory";
    public const string Node = $"{Prefix}Node";
    public const string Metadata = $"{Prefix}Metadata";
    public const string Client = $"{Prefix}Client";
    public const string Port = $"{Prefix}Port";
    public const string Device = $"{Prefix}Device";
    public const string Link = $"{Prefix}Link";
}
