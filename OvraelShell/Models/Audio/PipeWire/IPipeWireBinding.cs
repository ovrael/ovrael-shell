namespace OvraelShell.Models.Audio.PipeWire;

/// <summary>An object bound from the registry - destroyed by the connection before the core.</summary>
internal interface IPipeWireBinding
{
    /// <summary>Called by the connection - with the loop locked or stopped.</summary>
    void Destroy();
}
