using System.Runtime.InteropServices;

namespace OvraelShell.Models.Audio;

/// <summary>
/// struct pw_node_events and struct pw_device_events - both have the same shape,
/// only the info struct passed to <see cref="Info"/> differs.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct ObjectEvents
{
    public uint Version;
    public delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> Info;
    public delegate* unmanaged[Cdecl]<IntPtr, int, uint, uint, uint, IntPtr, void> Param;
}
