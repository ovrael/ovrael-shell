using Gio;
using GLib;
using OvraelShell.Enums.Network;
using OvraelShell.Models;
using OvraelShell.Models.Network;
using OvraelShell.Utils;

public class AudioDevice
{
    public uint Id { get; }
    public string Name { get; }

    public AudioDirection AudioDirection { get; set; } = AudioDirection.Unknown;
    public ReactiveProperty<bool> IsMuted { get; set; } = new();
    public ReactiveProperty<uint> Volume { get; set; } = new();
    public ReactiveProperty<int> VolumeLevel { get; set; } = new();

    public static int VolumeLevelOf(uint volume) =>
        volume switch
        {
            <= 0 => 0,
            < 30 => 1,
            < 70 => 2,
            _ => 3,
        };

    public AudioDevice() { }
}
