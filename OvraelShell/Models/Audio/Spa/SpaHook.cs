using System.Runtime.InteropServices;

namespace OvraelShell.Models.Audio.Spa;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct SpaHook
{
    public SpaList* LinkNext;
    public SpaList* LinkPrev;
    public IntPtr CallbacksFuncs;
    public IntPtr CallbacksData;
    public IntPtr Removed;
    public IntPtr Priv;
}
