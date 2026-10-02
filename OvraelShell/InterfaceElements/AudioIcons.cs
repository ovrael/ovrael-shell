using OvraelShell.Enums.Network;
using OvraelShell.Models.Audio.PipeWire;
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

    /// <summary>Speaker for outputs, microphone for inputs, a note for application streams.</summary>
    public static string Node(string? mediaClass) =>
        mediaClass switch
        {
            PipeWireMediaTypes.AudioSink => Icons.Audio.Output,
            PipeWireMediaTypes.AudioSource => Icons.Audio.Input,
            _ => Icons.Audio.Stream,
        };

    public static string Microphone(bool isMuted) =>
        isMuted ? Icons.Audio.InputMuted : Icons.Audio.Input;
}
