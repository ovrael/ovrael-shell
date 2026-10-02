using System.Runtime.InteropServices;

namespace OvraelShell.Models.Audio.Spa;

[StructLayout(LayoutKind.Sequential)]
public struct SpaDictItem
{
    public IntPtr Key;
    public IntPtr Value;
}
