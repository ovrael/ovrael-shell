using System.Runtime.InteropServices;

namespace OvraelShell.Models.Audio;

/// <summary>struct pw_metadata_events</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct MetadataEvents
{
    public uint Version;

    // subject, key, type, value - all strings may be null
    public delegate* unmanaged[Cdecl]<IntPtr, uint, IntPtr, IntPtr, IntPtr, int> Property;
}
