using OvraelShell.Models.Audio.PipeWire;

namespace OvraelShell.InterfaceElements;

public static class AudioIcons
{
    /// <param name="percent">On the scale of the sliders, 0-100.</param>
    public static string Volume(uint percent, bool isMuted)
    {
        if (isMuted)
            return Icons.Audio.VolumeOff;

        return percent switch
        {
            0 => Icons.Audio.VolumeNone,
            < 30 => Icons.Audio.VolumeLow,
            < 70 => Icons.Audio.VolumeMedium,
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
