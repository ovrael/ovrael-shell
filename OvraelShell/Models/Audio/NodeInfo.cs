using System.Runtime.InteropServices;
using OvraelShell.Models.Audio.Spa;

namespace OvraelShell.Models.Audio;

/// <summary>struct pw_node_info</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NodeInfo
{
    // PW_NODE_CHANGE_MASK_PROPS
    public const ulong ChangeMaskProps = 1 << 3;

    public uint Id;
    public uint MaxInputPorts;
    public uint MaxOutputPorts;
    public ulong ChangeMask;
    public uint InputPortCount;
    public uint OutputPortCount;
    public int State;
    public IntPtr Error;
    public SpaDict* Props;
    public IntPtr Params;
    public uint ParamCount;
}
