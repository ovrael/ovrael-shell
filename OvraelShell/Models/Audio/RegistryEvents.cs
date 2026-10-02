using System.Runtime.InteropServices;
using OvraelShell.Models.Audio.Spa;

namespace OvraelShell.Models.Audio;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct RegistryEvents
{
    public uint Version;
    public delegate* unmanaged[Cdecl]<IntPtr, uint, uint, IntPtr, uint, SpaDict*, void> Global;
    public delegate* unmanaged[Cdecl]<IntPtr, uint, void> GlobalRemove;
}
