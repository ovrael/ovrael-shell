using OvraelShell.Enums.Network;
using OvraelShell.Models.Network;

namespace OvraelShell.InterfaceElements;

public static class AudioIcons
{
    public static string Volume(uint volume, bool isMuted)
    {
        if (isMuted)
            return Icons.Audio.VolumeOff;

        return AudioDevice.VolumeLevelOf(volume) switch
        {
            0 => Icons.Audio.VolumeLow,
            1 => Icons.Audio.VolumeLow,
            2 => Icons.Audio.VolumeMedium,
            _ => Icons.Audio.VolumeHigh,
        };
    }
}
