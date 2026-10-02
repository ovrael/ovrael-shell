using System.Runtime.InteropServices;
using OvraelShell.Models.Audio.Spa;

namespace OvraelShell.Models.Audio;

/// <summary>struct pw_metadata_methods - the first argument of each is SpaInterface.Data.</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct MetadataMethods
{
    public uint Version;
    public delegate* unmanaged[Cdecl]<IntPtr, SpaHook*, MetadataEvents*, IntPtr, int> AddListener;
    public delegate* unmanaged[Cdecl]<IntPtr, uint, byte*, byte*, byte*, int> SetProperty;
    public delegate* unmanaged[Cdecl]<IntPtr, int> Clear;
}
