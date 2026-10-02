using System.Runtime.InteropServices;

namespace OvraelShell.Models.Audio.Spa;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct SpaDict
{
    public uint Flags;
    public uint ItemCount;
    public SpaDictItem* Items;
}
