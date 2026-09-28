using Gio;
using OvraelShell.Enums.Network;
using OvraelShell.Models;
using OvraelShell.Models.Network;
using OvraelShell.Utils;

namespace OvraelShell.Services;

public sealed class NetworkService : IDisposable
{
    // NM_STATE_CONNECTED_GLOBAL
    private const DeviceState NmStateConnectedGlobal = DeviceState.IpConfig;

    private readonly DBusProxy mainProxy;

    public ReactiveCollection<NetworkDevice> Devices { get; } = new();

    // Initialized before the constructor body, so devices can mark saved networks right away
    public SavedConnectionStore SavedConnections { get; } = new();

    public ReactiveProperty<bool> IsConnected { get; } = new();

    public ReactiveProperty<string?> PrimaryConnectionType { get; } = new();

    public NetworkDevice? PrimaryDevice =>
        Devices.Items.FirstOrDefault(device => device.ActiveConnectionPath.Value is not null);

    /// <summary>The first Wi-Fi device. Changes before a removed device is disposed.</summary>
    public ReactiveProperty<WirelessDevice?> WirelessDevice { get; } = new();

    public IReadOnlyList<EthernetDevice> EthernetDevices =>
        Devices.Items.OfType<EthernetDevice>().ToList();

    public NetworkService()
    {
        mainProxy = NetworkManagerBus.CreateProxy(
            NetworkManagerBus.MainPath,
            NetworkManagerBus.MainInterface
        );

        Devices.Changed += () =>
            WirelessDevice.Set(Devices.Items.OfType<WirelessDevice>().FirstOrDefault());

        ProxyGets.Bind(mainProxy, UpdateProperty, "State", "PrimaryConnectionType", "Devices");
    }

    public void RequestWifiScan() => WirelessDevice.Value?.RequestScan();

    /// <inheritdoc cref="WirelessDevice.Connect"/>
    public ConnectResult ConnectWireless(WirelessNetwork network, string? password = null) =>
        (
            WirelessDevice.Value ?? throw new InvalidOperationException("No Wi-Fi device found")
        ).Connect(network, password);

    private void UpdateProperty(string property)
    {
        switch (property)
        {
            case "State":
                IsConnected.Set(
                    ProxyGets.UInt(mainProxy, property) == (uint)NmStateConnectedGlobal
                );
                break;

            case "PrimaryConnectionType":
                PrimaryConnectionType.Set(ProxyGets.String(mainProxy, property));
                break;

            case "Devices":
                Devices.Sync(
                    ProxyGets.ObjectPathArray(mainProxy, property),
                    device => device.ObjectPath,
                    path => NetworkDevice.Create(path, SavedConnections),
                    device => device.Dispose()
                );
                break;
        }
    }

    public void Dispose()
    {
        foreach (var device in Devices.Items)
            device.Dispose();

        Devices.Clear();

        SavedConnections.Dispose();
        mainProxy.Dispose();
    }
}
