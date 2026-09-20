using System.Text;
using Gio;
using GLib;
using OvraelShell.Models;

namespace OvraelShell.Services;

public sealed class NetworkService : IDisposable
{
    private const string BusName = "org.freedesktop.NetworkManager";

    private const string MainPath = "/org/freedesktop/NetworkManager";

    private const string MainInterface = "org.freedesktop.NetworkManager";

    private const string ActiveConnectionInterface = $"{MainInterface}.Connection.Active";

    private const string DeviceInterface = $"{MainInterface}.Device";

    private const string WirelessInterface = $"{MainInterface}.Device.Wireless";

    private const string AccessPointInterface = $"{MainInterface}.AccessPoint";

    private const uint DeviceTypeWifi = 2;

    private DBusProxy? mainProxy;
    private DBusProxy? activeConnectionProxy;
    private DBusProxy? deviceProxy;
    private DBusProxy? wirelessProxy;
    private DBusProxy? accessPointProxy;

    private string? activeConnectionPath;
    private string? devicePath;
    private string? wirelessPath;
    private string? accessPointPath;

    public ReactiveProperty<DeviceState> State { get; } = new();

    public ReactiveProperty<string?> PrimaryConnectionType { get; } = new();

    public ReactiveProperty<bool> IsConnected { get; } = new();

    public ReactiveProperty<bool> IsWifi { get; } = new();

    public ReactiveProperty<string?> Ssid { get; } = new();

    public ReactiveProperty<byte> SignalStrength { get; } = new();
    public ReactiveProperty<IReadOnlyList<WifiNetwork>> AvailableNetworks { get; } = new();

    public NetworkService()
    {
        mainProxy = CreateProxy(MainPath, MainInterface);

        mainProxy.OnGPropertiesChanged += OnMainPropertiesChanged;

        UpdateMain();
    }

    public void RequestWifiScan()
    {
        if (wirelessProxy is null)
            return;

        using var key = VariantType.New("s");
        using var value = VariantType.New("v");
        using var dictEntryType = VariantType.NewDictEntry(key, value);

        using var options = Variant.NewArray(dictEntryType, []);

        using var parameters = Variant.NewTuple([options]);

        wirelessProxy.CallSync("RequestScan", parameters, DBusCallFlags.None, -1, null);
    }

    #region Proxy creation

    private static DBusProxy CreateProxy(string path, string interfaceName)
    {
        return DBusProxy.NewForBusSync(
            BusType.System,
            DBusProxyFlags.None,
            null,
            BusName,
            path,
            interfaceName,
            null
        );
    }

    #endregion

    #region NetworkManager

    private void UpdateMain()
    {
        if (mainProxy is null)
            return;

        var proxyState = GetUInt32(mainProxy, "State");
        if (!Enum.TryParse($"{proxyState}", true, out DeviceState state))
        {
            state = DeviceState.Unknown;
        }

        var connectionType = GetString(mainProxy, "PrimaryConnectionType");

        State.Set(state);
        PrimaryConnectionType.Set(connectionType);

        IsConnected.Set(state == DeviceState.IpConfig);

        IsWifi.Set(connectionType == "802-11-wireless");

        if (state != DeviceState.IpConfig)
        {
            ClearConnection();
            return;
        }

        var path = GetObjectPath(mainProxy, "PrimaryConnection");

        if (string.IsNullOrEmpty(path) || path == "/")
        {
            ClearConnection();
            return;
        }

        UpdateActiveConnection(path);
    }

    private void OnMainPropertiesChanged(
        DBusProxy sender,
        DBusProxy.GPropertiesChangedSignalArgs args
    )
    {
        foreach (var property in GetChangedProperties(args.ChangedProperties))
        {
            System.Console.WriteLine($"NetworkManager: {property}");

            switch (property)
            {
                case "State":
                    UpdateState(sender);
                    break;

                case "PrimaryConnectionType":
                    UpdatePrimaryConnectionType(sender);
                    break;

                case "PrimaryConnection":
                    var path = GetObjectPath(sender, "PrimaryConnection");

                    if (string.IsNullOrEmpty(path) || path == "/")
                    {
                        ClearConnection();
                    }
                    else
                    {
                        UpdateActiveConnection(path);
                    }

                    break;
            }
        }
    }

    private void UpdateState(DBusProxy proxy)
    {
        var proxyState = GetUInt32(proxy, "State");
        if (!Enum.TryParse($"{proxyState}", true, out DeviceState state))
        {
            state = DeviceState.Unknown;
        }

        State.Set(state);

        var connected = state == DeviceState.IpConfig;

        IsConnected.Set(connected);

        if (!connected)
            ClearConnection();
    }

    private void UpdatePrimaryConnectionType(DBusProxy proxy)
    {
        var type = GetString(proxy, "PrimaryConnectionType");

        PrimaryConnectionType.Set(type);
        IsWifi.Set(type == "802-11-wireless");

        if (type != "802-11-wireless")
            ClearWifi();
    }

    #endregion

    #region Active connection

    private void UpdateActiveConnection(string path)
    {
        if (activeConnectionPath == path && activeConnectionProxy is not null)
        {
            UpdateActiveConnectionDevices(activeConnectionProxy);

            return;
        }

        DisposeActiveConnection();

        activeConnectionPath = path;

        activeConnectionProxy = CreateProxy(path, ActiveConnectionInterface);

        activeConnectionProxy.OnGPropertiesChanged += OnActiveConnectionPropertiesChanged;

        UpdateActiveConnectionDevices(activeConnectionProxy);
    }

    private void OnActiveConnectionPropertiesChanged(
        DBusProxy sender,
        DBusProxy.GPropertiesChangedSignalArgs args
    )
    {
        foreach (var property in GetChangedProperties(args.ChangedProperties))
        {
            System.Console.WriteLine($"ActiveConnection: {property}");

            switch (property)
            {
                case "Devices":
                    UpdateActiveConnectionDevices(sender);
                    break;
            }
        }
    }

    private void UpdateActiveConnectionDevices(DBusProxy proxy)
    {
        var devices = GetObjectPathArray(proxy, "Devices");

        if (devices.Length == 0)
        {
            DisposeDevice();
            return;
        }

        // Na ten moment interesuje nas pierwszy aktywny device.
        var path = devices[0];

        if (string.IsNullOrEmpty(path) || path == "/")
        {
            DisposeDevice();
            return;
        }

        UpdateDevice(path);
    }

    #endregion

    #region Device

    private void UpdateDevice(string path)
    {
        if (devicePath == path && deviceProxy is not null)
        {
            UpdateDeviceType(deviceProxy);
            return;
        }

        DisposeDevice();

        devicePath = path;

        deviceProxy = CreateProxy(path, DeviceInterface);

        deviceProxy.OnGPropertiesChanged += OnDevicePropertiesChanged;

        UpdateDeviceType(deviceProxy);
    }

    private void UpdateDeviceType(DBusProxy proxy)
    {
        var deviceType = GetUInt32(proxy, "DeviceType");

        // NM_DEVICE_TYPE_WIFI = 2
        if (deviceType != DeviceTypeWifi)
        {
            ClearWifi();
            return;
        }

        UpdateWireless();
    }

    private void OnDevicePropertiesChanged(
        DBusProxy sender,
        DBusProxy.GPropertiesChangedSignalArgs args
    )
    {
        foreach (var property in GetChangedProperties(args.ChangedProperties))
        {
            System.Console.WriteLine($"Device: {property}");

            switch (property)
            {
                case "DeviceType":
                    UpdateDeviceType(sender);
                    break;
            }
        }
    }

    #endregion

    #region Wireless

    private void UpdateWireless()
    {
        if (devicePath is null)
            return;

        if (wirelessPath == devicePath && wirelessProxy is not null)
        {
            UpdateActiveAccessPoint(wirelessProxy);
            UpdateAvailableNetworks(wirelessProxy);

            return;
        }

        DisposeWireless();

        wirelessPath = devicePath;

        wirelessProxy = CreateProxy(wirelessPath, WirelessInterface);

        wirelessProxy.OnGPropertiesChanged += OnWirelessPropertiesChanged;

        UpdateActiveAccessPoint(wirelessProxy);
        UpdateAvailableNetworks(wirelessProxy);
    }

    private void UpdateAvailableNetworks(DBusProxy proxy)
    {
        var accessPoints = GetObjectPathArray(proxy, "AccessPoints");
        var networks = new List<WifiNetwork>(accessPoints.Length);

        foreach (var path in accessPoints)
        {
            if (string.IsNullOrEmpty(path) || path == "/")
                continue;

            using var accessPoint = CreateProxy(path, AccessPointInterface);

            var ssid = GetSsid(accessPoint);

            if (string.IsNullOrEmpty(ssid))
                continue;

            var bssid = GetString(accessPoint, "HwAddress");
            var strength = GetByte(accessPoint, "Strength");
            var frequency = GetUInt32(accessPoint, "Frequency");
            var flags = GetUInt32(accessPoint, "Flags");
            var wpaFlags = GetUInt32(accessPoint, "WpaFlags");
            var rsnFlags = GetUInt32(accessPoint, "RsnFlags");

            networks.Add(
                new WifiNetwork(
                    Ssid: ssid,
                    Bssid: bssid ?? string.Empty,
                    Strength: strength,
                    Frequency: frequency,
                    Flags: flags,
                    WpaFlags: wpaFlags,
                    RsnFlags: rsnFlags,
                    ObjectPath: path
                )
            );
        }

        AvailableNetworks.Set(networks);
    }

    private void OnWirelessPropertiesChanged(
        DBusProxy sender,
        DBusProxy.GPropertiesChangedSignalArgs args
    )
    {
        foreach (var property in GetChangedProperties(args.ChangedProperties))
        {
            System.Console.WriteLine($"Wireless: {property}");

            switch (property)
            {
                case "ActiveAccessPoint":
                    UpdateActiveAccessPoint(sender);
                    break;

                case "AccessPoints":
                    UpdateAvailableNetworks(sender);
                    break;
            }
        }
    }

    private void UpdateActiveAccessPoint(DBusProxy wirelessProxy)
    {
        var path = GetObjectPath(wirelessProxy, "ActiveAccessPoint");

        if (string.IsNullOrEmpty(path) || path == "/")
        {
            DisposeAccessPoint();
            ClearWifi();
            return;
        }

        if (accessPointPath == path && accessPointProxy is not null)
        {
            UpdateAccessPointValues(accessPointProxy);

            return;
        }

        DisposeAccessPoint();

        accessPointPath = path;

        accessPointProxy = CreateProxy(path, AccessPointInterface);

        accessPointProxy.OnGPropertiesChanged += OnAccessPointPropertiesChanged;

        UpdateAccessPointValues(accessPointProxy);
    }

    #endregion

    #region Access point

    private void UpdateAccessPointValues(DBusProxy proxy)
    {
        Ssid.Set(GetSsid(proxy));

        SignalStrength.Set(GetByte(proxy, "Strength"));
    }

    private void OnAccessPointPropertiesChanged(
        DBusProxy sender,
        DBusProxy.GPropertiesChangedSignalArgs args
    )
    {
        foreach (var property in GetChangedProperties(args.ChangedProperties))
        {
            System.Console.WriteLine($"AccessPoint: {property}");

            switch (property)
            {
                case "Strength":
                    SignalStrength.Set(GetByte(sender, "Strength"));
                    break;

                case "Ssid":
                    Ssid.Set(GetSsid(sender));
                    break;
            }
        }
    }

    #endregion

    #region Cleanup

    private void ClearConnection()
    {
        DisposeActiveConnection();
        ClearWifi();
    }

    private void ClearWifi()
    {
        Ssid.Set(null);
        SignalStrength.Set(0);
        AvailableNetworks.Set([]);

        DisposeWireless();
    }

    private void DisposeActiveConnection()
    {
        if (activeConnectionProxy is not null)
        {
            activeConnectionProxy.OnGPropertiesChanged -= OnActiveConnectionPropertiesChanged;

            activeConnectionProxy.Dispose();
            activeConnectionProxy = null;
        }

        activeConnectionPath = null;

        DisposeDevice();
    }

    private void DisposeDevice()
    {
        if (deviceProxy is not null)
        {
            deviceProxy.OnGPropertiesChanged -= OnDevicePropertiesChanged;

            deviceProxy.Dispose();
            deviceProxy = null;
        }

        devicePath = null;

        DisposeWireless();
    }

    private void DisposeWireless()
    {
        if (wirelessProxy is not null)
        {
            wirelessProxy.OnGPropertiesChanged -= OnWirelessPropertiesChanged;

            wirelessProxy.Dispose();
            wirelessProxy = null;
        }

        wirelessPath = null;

        DisposeAccessPoint();
    }

    private void DisposeAccessPoint()
    {
        if (accessPointProxy is not null)
        {
            accessPointProxy.OnGPropertiesChanged -= OnAccessPointPropertiesChanged;

            accessPointProxy.Dispose();
            accessPointProxy = null;
        }

        accessPointPath = null;
    }

    #endregion

    #region Variant helpers

    private static IEnumerable<string> GetChangedProperties(Variant properties)
    {
        var count = properties.NChildren();

        for (nuint i = 0; i < count; i++)
        {
            using var entry = properties.GetChildValue(i);

            using var key = entry.GetChildValue(0);

            yield return key.GetString(out _);
        }
    }

    private static uint GetUInt32(DBusProxy proxy, string property)
    {
        var variant = proxy.GetCachedProperty(property);

        if (variant is null)
            return 0;

        return variant.GetUint32();
    }

    private static byte GetByte(DBusProxy proxy, string property)
    {
        var variant = proxy.GetCachedProperty(property);

        if (variant is null)
            return 0;

        return variant.GetByte();
    }

    private static string? GetString(DBusProxy proxy, string property)
    {
        var variant = proxy.GetCachedProperty(property);

        if (variant is null)
            return null;

        return variant.GetString(out _);
    }

    private static string? GetObjectPath(DBusProxy proxy, string property)
    {
        var variant = proxy.GetCachedProperty(property);

        if (variant is null)
            return null;

        return variant.GetString(out _);
    }

    private static string[] GetObjectPathArray(DBusProxy proxy, string property)
    {
        var variant = proxy.GetCachedProperty(property);

        if (variant is null)
            return [];

        return variant.GetObjv(out _);
    }

    private static string? GetSsid(DBusProxy proxy)
    {
        var variant = proxy.GetCachedProperty("Ssid");

        if (variant is null)
            return null;

        using var bytes = variant.GetDataAsBytes();

        var data = bytes.GetRegionSpan<byte>(0, bytes.GetSize());

        return Encoding.UTF8.GetString(data);
    }

    #endregion

    #region Dispose

    public void Dispose()
    {
        if (mainProxy is not null)
        {
            mainProxy.OnGPropertiesChanged -= OnMainPropertiesChanged;

            mainProxy.Dispose();
            mainProxy = null;
        }

        DisposeActiveConnection();
    }
    #endregion
}
