using Gio;
using OvraelShell.Enums.Network;
using OvraelShell.Models;
using OvraelShell.Models.Network;
using OvraelShell.Utils;

namespace OvraelShell.Services;

public sealed class AudioService : IDisposable
{
    public ReactiveProperty<AudioDevice?> ActiveDevice { get; set; } = new();

    public AudioService() { }

    public void Dispose() { }
}
