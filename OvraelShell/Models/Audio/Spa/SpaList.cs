using System.Runtime.InteropServices;

namespace OvraelShell.Models.Audio.Spa;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct SpaList
{
    public SpaList* Next;
    public SpaList* Prev;
}
