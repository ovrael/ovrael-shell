using Gio;
using GLib;
using Task = System.Threading.Tasks.Task;
using OvraelShell.Models;
using OvraelShell.Models.Bluetooth;
using OvraelShell.Utils;

namespace OvraelShell.Services;

/// <summary>
/// Follows BlueZ through its object manager - adapters, devices and their batteries
/// come and go as D-Bus objects. Starts again when bluetoothd restarts.
/// </summary>
public sealed class BluetoothService : IDisposable
{
    private readonly DBusProxy objectManager;
    private readonly List<BluetoothAdapter> adapters = [];

    // Battery1 can come before Device1 is handled - kept until the device exists
    private readonly HashSet<string> batteryPaths = [];

    private bool discoveryWanted;
    private bool discoveryStarted;

    /// <summary>The first adapter. Null when there is none or BlueZ is not running.</summary>
    public ReactiveProperty<BluetoothAdapter?> Adapter { get; } = new();

    public ReactiveCollection<BluetoothDevice> Devices { get; } = new();

    public ReactiveProperty<IReadOnlyList<BluetoothDevice>> ConnectedDevices { get; } =
        new([], SequenceComparer<BluetoothDevice>.Instance);

    public BluetoothService()
    {
        // Not started for us - when bluetoothd is off, the owner is null until it starts
        objectManager = BluezBus.CreateProxy(
            BluezBus.RootPath,
            BluezBus.ObjectManagerInterface,
            DBusProxyFlags.DoNotLoadProperties | DBusProxyFlags.DoNotAutoStart
        );

        objectManager.OnGSignal += OnObjectManagerSignal;
        objectManager.OnNotify += (_, args) =>
        {
            if (args.Pspec.GetName() == "g-name-owner")
                Reload();
        };

        Adapter.Changed += OnAdapterChanged;

        Reload();
    }

    /// <summary>
    /// Searches for new devices while wanted (the popover is open) and the adapter is on.
    /// </summary>
    public void SetDiscovery(bool wanted)
    {
        discoveryWanted = wanted;
        _ = UpdateDiscoveryAsync();
    }

    #region Objects

    private void Reload()
    {
        Clear();

        if (objectManager.GetNameOwner() is null)
        {
            Console.WriteLine("Bluetooth: BlueZ is not running.");
            return;
        }

        try
        {
            using var result = objectManager.CallSync(
                "GetManagedObjects",
                null,
                DBusCallFlags.None,
                -1,
                null
            );
            using var objects = result.GetChildValue(0);

            // a{oa{sa{sv}}}
            foreach (var (path, interfaces) in VariantGets.Entries(objects))
            {
                AddInterfaces(path, InterfaceNames(interfaces));
                interfaces.Dispose();
            }
        }
        catch (GException exception)
        {
            Console.WriteLine($"Bluetooth: cannot read the BlueZ objects: {exception.Message}");
        }
    }

    private void OnObjectManagerSignal(DBusProxy sender, DBusProxy.GSignalSignalArgs args)
    {
        using var parameters = args.Parameters;
        using var pathVariant = parameters.GetChildValue(0);
        using var interfaces = parameters.GetChildValue(1);

        var path = pathVariant.GetString(out _);

        switch (args.SignalName)
        {
            // (oa{sa{sv}})
            case "InterfacesAdded":
                AddInterfaces(path, InterfaceNames(interfaces));
                break;

            // (oas)
            case "InterfacesRemoved":
                RemoveInterfaces(path, interfaces.GetStrv(out _));
                break;
        }
    }

    private static string[] InterfaceNames(Variant interfaces) =>
        VariantGets
            .Entries(interfaces)
            .Select(entry =>
            {
                entry.Value.Dispose();
                return entry.Key;
            })
            .ToArray();

    // Device before Battery - the battery is attached to the device
    private void AddInterfaces(string path, string[] interfaces)
    {
        if (interfaces.Contains(BluezBus.AdapterInterface) && FindAdapter(path) is null)
        {
            adapters.Add(new BluetoothAdapter(path));
            Adapter.Set(adapters[0]);
        }

        if (interfaces.Contains(BluezBus.DeviceInterface) && FindDevice(path) is null)
            AddDevice(path);

        if (interfaces.Contains(BluezBus.BatteryInterface))
        {
            batteryPaths.Add(path);
            FindDevice(path)?.AttachBattery();
        }
    }

    private void RemoveInterfaces(string path, string[] interfaces)
    {
        if (interfaces.Contains(BluezBus.BatteryInterface))
        {
            batteryPaths.Remove(path);
            FindDevice(path)?.DetachBattery();
        }

        if (interfaces.Contains(BluezBus.DeviceInterface) && FindDevice(path) is { } device)
            RemoveDevice(device);

        if (interfaces.Contains(BluezBus.AdapterInterface) && FindAdapter(path) is { } adapter)
        {
            adapters.Remove(adapter);

            // Before Dispose - the followers let go of the old adapter first
            Adapter.Set(adapters.FirstOrDefault());
            adapter.Dispose();
        }
    }

    private void AddDevice(string path)
    {
        var device = BluetoothDevice.Create(path);
        device.Connected.Changed += OnDeviceConnectedChanged;

        if (batteryPaths.Contains(path))
            device.AttachBattery();

        Devices.Add(device);
        UpdateConnectedDevices();
    }

    private void RemoveDevice(BluetoothDevice device)
    {
        device.Connected.Changed -= OnDeviceConnectedChanged;

        Devices.Remove(device);
        device.Release();
        UpdateConnectedDevices();
    }

    private BluetoothAdapter? FindAdapter(string path) =>
        adapters.FirstOrDefault(adapter => adapter.ObjectPath == path);

    private BluetoothDevice? FindDevice(string path) =>
        Devices.Items.FirstOrDefault(device => device.ObjectPath == path);

    private void OnDeviceConnectedChanged(bool _) => UpdateConnectedDevices();

    private void UpdateConnectedDevices() =>
        ConnectedDevices.Set(Devices.Items.Where(device => device.Connected.Value).ToArray());

    private void Clear()
    {
        foreach (var device in Devices.Items.ToArray())
            RemoveDevice(device);

        batteryPaths.Clear();

        Adapter.Set(null);

        foreach (var adapter in adapters)
            adapter.Dispose();

        adapters.Clear();
    }

    #endregion

    #region Discovery

    private BluetoothAdapter? discoveryAdapter;

    private void OnAdapterChanged(BluetoothAdapter? adapter)
    {
        discoveryAdapter?.Powered.Changed -= OnPoweredChanged;
        discoveryAdapter = adapter;
        discoveryAdapter?.Powered.Changed += OnPoweredChanged;

        // A new adapter has no discovery of ours
        discoveryStarted = false;
        _ = UpdateDiscoveryAsync();
    }

    private void OnPoweredChanged(bool powered) => _ = UpdateDiscoveryAsync();

    private async Task UpdateDiscoveryAsync()
    {
        if (Adapter.Value is not { } adapter)
            return;

        // Powering off stops the discovery
        if (!adapter.Powered.Value)
        {
            discoveryStarted = false;
            return;
        }

        if (discoveryWanted == discoveryStarted)
            return;

        var start = discoveryWanted;
        discoveryStarted = start;

        try
        {
            if (start)
                await adapter.StartDiscoveryAsync();
            else
                await adapter.StopDiscoveryAsync();
        }
        catch (GException exception)
        {
            Console.WriteLine(
                $"Bluetooth: cannot {(start ? "start" : "stop")} the discovery: {exception.Message}"
            );

            if (start)
                discoveryStarted = false;
        }
    }

    #endregion

    public void Dispose()
    {
        // BlueZ stops the discovery of a client that leaves the bus
        objectManager.OnGSignal -= OnObjectManagerSignal;
        Adapter.Changed -= OnAdapterChanged;

        Clear();
        objectManager.Dispose();
    }
}
