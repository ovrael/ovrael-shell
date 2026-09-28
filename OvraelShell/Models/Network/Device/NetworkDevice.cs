using Gio;
using GLib;
using OvraelShell.Enums.Network;
using OvraelShell.Models;
using OvraelShell.Models.Network;
using OvraelShell.Utils;

public abstract class NetworkDevice : IWithObjectPath, IDisposable
{
    protected DBusProxy Proxy { get; }

    public string ObjectPath { get; }

    public string InterfaceName { get; }
    public string MacAddress { get; }

    public ReactiveProperty<DeviceState> State { get; } = new();

    /// <summary>Why the device entered its current state - updated before <see cref="State"/>.</summary>
    public ReactiveProperty<DeviceStateReason> StateReason { get; } = new();
    public ReactiveProperty<string?> ActiveConnectionPath { get; } = new();

    protected NetworkDevice(string objectPath, DBusProxy proxy)
    {
        ObjectPath = objectPath;
        Proxy = proxy;

        InterfaceName = ProxyGets.String(proxy, "Interface") ?? string.Empty;
        MacAddress = ProxyGets.String(proxy, "HwAddress") ?? string.Empty;

        ProxyGets.Bind(proxy, UpdateProperty, "State", "ActiveConnection");
    }

    /// <summary>Creates the model matching the device type, or null if the type is not supported.</summary>
    public static NetworkDevice? Create(string objectPath, SavedConnectionStore savedConnections)
    {
        var proxy = NetworkManagerBus.CreateProxy(objectPath, NetworkManagerBus.DeviceInterface);

        NetworkDevice? device = (DeviceType)ProxyGets.UInt(proxy, "DeviceType") switch
        {
            DeviceType.Wifi => new WirelessDevice(objectPath, proxy, savedConnections),

            DeviceType.Ethernet => new EthernetDevice(objectPath, proxy),

            _ => null,
        };

        if (device is null)
            proxy.Dispose();

        return device;
    }

    /// <summary>Whether <paramref name="saved"/> is the connection currently active on this device. Read-only.</summary>
    public bool IsActiveProfile(SavedConnection saved)
    {
        var activePath = ActiveConnectionPath.Value;

        if (activePath is null)
            return false;

        try
        {
            using var active = NetworkManagerBus.CreateProxy(
                activePath,
                NetworkManagerBus.ActiveConnectionInterface
            );

            return ProxyGets.ObjectPath(active, "Connection") == saved.ObjectPath;
        }
        catch (GException)
        {
            // Active connection vanished in the meantime
            return false;
        }
    }

    private void UpdateProperty(string property)
    {
        switch (property)
        {
            case "State":
                // Both arrive in one PropertiesChanged and the cache already has the new
                // reason, so State listeners can read it
                StateReason.Set((DeviceStateReason)ProxyGets.UIntAt(Proxy, "StateReason", 1));
                State.Set((DeviceState)ProxyGets.UInt(Proxy, property));
                break;

            case "ActiveConnection":
                ActiveConnectionPath.Set(ProxyGets.ObjectPath(Proxy, property));
                break;
        }
    }

    public virtual void Dispose()
    {
        Proxy.Dispose();
    }
}
