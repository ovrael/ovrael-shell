using Gio;
using GLib;
using Task = System.Threading.Tasks.Task;
using OvraelShell.Interfaces;
using OvraelShell.Utils;

namespace OvraelShell.Models.Bluetooth;

/// <summary>A paired or found Bluetooth device (org.bluez.Device1).</summary>
[GObject.Subclass<GObject.Object>]
public sealed partial class BluetoothDevice
    : IWithObjectPath,
        IReactivable<BluetoothDevice>,
        IEquatable<BluetoothDevice>
{
    private DBusProxy? proxy;
    private DBusProxy? batteryProxy;

    /// <summary>Identity of the device - one object in BlueZ.</summary>
    public string ObjectPath { get; private set; } = string.Empty;

    public string AdapterPath { get; private set; } = string.Empty;

    public string Address { get; private set; } = string.Empty;

    /// <summary>Alias - the name given by the user or by the device, else the address.</summary>
    public ReactiveProperty<string> Name { get; } = new(string.Empty);

    /// <summary>The name the device sends. Empty until it sends one.</summary>
    public ReactiveProperty<string> RemoteName { get; } = new(string.Empty);

    /// <summary>The device sent its name. Found devices without one are mostly LE beacons.</summary>
    public ReactiveProperty<bool> HasName { get; } = new();

    /// <summary>The user gave the device a name of their own.</summary>
    public bool IsRenamed => HasName.Value && Name.Value != RemoteName.Value;

    /// <summary>Freedesktop icon name, e.g. audio-headset, input-gaming. Empty if unknown.</summary>
    public ReactiveProperty<string> Icon { get; } = new(string.Empty);

    public ReactiveProperty<bool> Paired { get; } = new();

    public ReactiveProperty<bool> Connected { get; } = new();

    public ReactiveProperty<bool> Trusted { get; } = new();

    /// <summary>Battery in percent. Null when the device does not report it.</summary>
    public ReactiveProperty<byte?> Battery { get; } = new();

    /// <summary>Pair, connect, disconnect, rename or forget is running.</summary>
    public ReactiveProperty<bool> IsBusy { get; } = new();

    /// <summary>"Connecting..." or the last error; null when there is nothing to show.</summary>
    public ReactiveProperty<string?> Status { get; } = new();

    public event Action<BluetoothDevice>? OnChange;

    partial void Initialize()
    {
        Name.Changed += (_) => OnChange?.Invoke(this);
        RemoteName.Changed += (_) => OnChange?.Invoke(this);
        HasName.Changed += (_) => OnChange?.Invoke(this);
        Icon.Changed += (_) => OnChange?.Invoke(this);
        Paired.Changed += (_) => OnChange?.Invoke(this);
        Connected.Changed += (_) => OnChange?.Invoke(this);
        Trusted.Changed += (_) => OnChange?.Invoke(this);
        Battery.Changed += (_) => OnChange?.Invoke(this);
        IsBusy.Changed += (_) => OnChange?.Invoke(this);
        Status.Changed += (_) => OnChange?.Invoke(this);
    }

    public static BluetoothDevice Create(string objectPath)
    {
        var proxy = BluezBus.CreateProxy(objectPath, BluezBus.DeviceInterface);

        var device = NewWithProperties([]);
        device.proxy = proxy;
        device.ObjectPath = objectPath;
        device.AdapterPath = ProxyGets.ObjectPath(proxy, "Adapter") ?? string.Empty;
        device.Address = ProxyGets.String(proxy, "Address") ?? string.Empty;

        ProxyGets.Bind(
            proxy,
            device.UpdateProperty,
            "Alias",
            "Name",
            "Icon",
            "Paired",
            "Connected",
            "Trusted"
        );

        return device;
    }

    private void UpdateProperty(string property)
    {
        if (proxy is null)
            return;

        switch (property)
        {
            case "Alias":
                Name.Set(ProxyGets.String(proxy, property) ?? Address);
                break;

            // Missing until the device sends it
            case "Name":
                var remoteName = ProxyGets.String(proxy, property);
                RemoteName.Set(remoteName ?? string.Empty);
                HasName.Set(remoteName is not null);
                break;

            case "Icon":
                Icon.Set(ProxyGets.String(proxy, property) ?? string.Empty);
                break;

            case "Paired":
                Paired.Set(ProxyGets.Bool(proxy, property) ?? false);
                break;

            case "Connected":
                Connected.Set(ProxyGets.Bool(proxy, property) ?? false);
                break;

            case "Trusted":
                Trusted.Set(ProxyGets.Bool(proxy, property) ?? false);
                break;
        }
    }

    #region Battery

    /// <summary>BlueZ added org.bluez.Battery1 to the device - usually after it connects.</summary>
    public void AttachBattery()
    {
        if (batteryProxy is not null)
            return;

        batteryProxy = BluezBus.CreateProxy(ObjectPath, BluezBus.BatteryInterface);
        ProxyGets.Bind(batteryProxy, UpdateBattery, "Percentage");
    }

    public void DetachBattery()
    {
        batteryProxy?.Dispose();
        batteryProxy = null;
        Battery.Set(null);
    }

    private void UpdateBattery(string property)
    {
        if (batteryProxy?.GetCachedProperty(property) is { } variant)
            Battery.Set(variant.GetByte());
    }

    #endregion

    #region Actions

    /// <summary>
    /// Connects. A device that is not paired is paired first and trusted, so it can connect
    /// again by itself. Progress and errors are shown in <see cref="Status"/>.
    /// </summary>
    public Task ConnectAsync() =>
        RunAsync(async () =>
        {
            if (!Paired.Value)
            {
                Status.Set("Pairing...");
                await CallAsync("Pair");
            }

            if (!Trusted.Value)
            {
                await BluezBus.SetPropertyAsync(
                    ObjectPath,
                    BluezBus.DeviceInterface,
                    "Trusted",
                    Variant.NewBoolean(true)
                );
            }

            Status.Set("Connecting...");
            await CallAsync("Connect");
        });

    public Task DisconnectAsync() =>
        RunAsync(async () =>
        {
            Status.Set("Disconnecting...");
            await CallAsync("Disconnect");
        });

    /// <summary>
    /// Gives the device a name of its own (the Alias, kept by BlueZ).
    /// An empty <paramref name="name"/> goes back to the name the device sends.
    /// </summary>
    public Task RenameAsync(string name) =>
        RunAsync(() =>
            BluezBus.SetPropertyAsync(
                ObjectPath,
                BluezBus.DeviceInterface,
                "Alias",
                Variant.NewString(name.Trim())
            )
        );

    /// <summary>Removes the pairing. BlueZ removes the device too, until it is found again.</summary>
    public Task ForgetAsync() =>
        RunAsync(async () =>
        {
            Status.Set("Removing...");
            await BluezBus.CallAsync(
                AdapterPath,
                BluezBus.AdapterInterface,
                "RemoveDevice",
                Variant.NewObjectPath(ObjectPath)
            );
        });

    private Task CallAsync(string method) =>
        BluezBus.CallAsync(ObjectPath, BluezBus.DeviceInterface, method);

    // One action at a time - the buttons are disabled while one runs
    private async Task RunAsync(Func<Task> action)
    {
        if (IsBusy.Value)
            return;

        IsBusy.Set(true);
        try
        {
            await action();
            Status.Set(null);
        }
        catch (GException exception)
        {
            Console.WriteLine($"Bluetooth: {Name.Value} ({Address}): {exception.Message}");
            Status.Set(BluezBus.ErrorMessage(exception));
        }
        finally
        {
            IsBusy.Set(false);
        }
    }

    #endregion

    /// <summary>
    /// Releases the D-Bus proxies. Not named Dispose, because that would free the GObject,
    /// which may still be held by a widget's ListStore.
    /// </summary>
    public void Release()
    {
        proxy?.Dispose();
        proxy = null;
        DetachBattery();
    }

    #region Equality

    // Wrappers from GTK models (ListStore, ListItem) are compared by path, not by reference

    public bool Equals(BluetoothDevice? other) =>
        other is not null && ObjectPath == other.ObjectPath;

    public override bool Equals(object? obj) => Equals(obj as BluetoothDevice);

    public override int GetHashCode() => ObjectPath.GetHashCode();

    public static bool operator ==(BluetoothDevice? left, BluetoothDevice? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(BluetoothDevice? left, BluetoothDevice? right) =>
        !(left == right);

    #endregion

    public override string ToString() =>
        $"BluetoothDevice {{ Name = {Name}, Address = {Address}, Paired = {Paired}, "
        + $"Connected = {Connected}, Battery = {Battery}, ObjectPath = {ObjectPath} }}";
}
