using Gio;
using GLib;
using Task = System.Threading.Tasks.Task;
using OvraelShell.Utils;

namespace OvraelShell.Models.Bluetooth;

/// <summary>A Bluetooth controller (org.bluez.Adapter1).</summary>
public sealed class BluetoothAdapter : IWithObjectPath, IDisposable
{
    private readonly DBusProxy proxy;

    public string ObjectPath { get; }

    public ReactiveProperty<bool> Powered { get; } = new();

    public ReactiveProperty<bool> Discovering { get; } = new();

    /// <summary>"off-blocked" when rfkill blocks the adapter. Empty before BlueZ 5.66.</summary>
    public ReactiveProperty<string> PowerState { get; } = new(string.Empty);

    public BluetoothAdapter(string objectPath)
    {
        ObjectPath = objectPath;
        proxy = BluezBus.CreateProxy(objectPath, BluezBus.AdapterInterface);

        ProxyGets.Bind(proxy, UpdateProperty, "Powered", "Discovering", "PowerState");
    }

    public Task SetPoweredAsync(bool powered) =>
        BluezBus.SetPropertyAsync(
            ObjectPath,
            BluezBus.AdapterInterface,
            "Powered",
            Variant.NewBoolean(powered)
        );

    public Task StartDiscoveryAsync() =>
        BluezBus.CallAsync(ObjectPath, BluezBus.AdapterInterface, "StartDiscovery");

    public Task StopDiscoveryAsync() =>
        BluezBus.CallAsync(ObjectPath, BluezBus.AdapterInterface, "StopDiscovery");

    private void UpdateProperty(string property)
    {
        switch (property)
        {
            case "Powered":
                Powered.Set(ProxyGets.Bool(proxy, property) ?? false);
                break;

            case "Discovering":
                Discovering.Set(ProxyGets.Bool(proxy, property) ?? false);
                break;

            case "PowerState":
                PowerState.Set(ProxyGets.String(proxy, property) ?? string.Empty);
                break;
        }
    }

    public void Dispose() => proxy.Dispose();
}
