using System.Runtime.InteropServices;

namespace OvraelShell.Models.Audio.Spa;

/// <summary>
/// struct spa_interface - the first member of every proxy. Holds the method table, used for
/// the methods the library does not export (the pw_metadata_* ones are inline in the headers).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct SpaInterface
{
    public IntPtr Type;
    public uint Version;

    // struct spa_callbacks
    public IntPtr Funcs;
    public IntPtr Data;
}
