using Gio;
using OvraelShell.Models;
using OvraelShell.Utils;

public sealed class EthernetDevice : NetworkDevice
{
    // Carrier and Speed live on the Device.Wired interface
    private readonly DBusProxy wiredProxy;

    public ReactiveProperty<bool> Carrier { get; } = new();
    public ReactiveProperty<uint> Speed { get; } = new();

    public EthernetDevice(string objectPath, DBusProxy proxy)
        : base(objectPath, proxy)
    {
        wiredProxy = NetworkManagerBus.CreateProxy(objectPath, NetworkManagerBus.WiredInterface);

        ProxyGets.Bind(wiredProxy, UpdateProperty, "Carrier", "Speed");
    }

    private void UpdateProperty(string property)
    {
        switch (property)
        {
            case "Carrier":
                Carrier.Set(ProxyGets.Bool(wiredProxy, property) ?? false);
                break;

            case "Speed":
                Speed.Set(ProxyGets.UInt(wiredProxy, property));
                break;
        }
    }

    public override void Dispose()
    {
        wiredProxy.Dispose();
        base.Dispose();
    }
}
