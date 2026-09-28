using Gio;
using GLib;

namespace OvraelShell.Utils;

public static class NetworkManagerBus
{
    public const string BusName = "org.freedesktop.NetworkManager";

    public const string MainPath = "/org/freedesktop/NetworkManager";

    public const string MainInterface = "org.freedesktop.NetworkManager";

    public const string DeviceInterface = $"{MainInterface}.Device";

    public const string WiredInterface = $"{DeviceInterface}.Wired";

    public const string WirelessInterface = $"{DeviceInterface}.Wireless";

    public const string AccessPointInterface = $"{MainInterface}.AccessPoint";

    public const string ActiveConnectionInterface = $"{MainInterface}.Connection.Active";

    public const string SettingsPath = $"{MainPath}/Settings";

    public const string SettingsInterface = $"{MainInterface}.Settings";

    public const string SettingsConnectionInterface = $"{SettingsInterface}.Connection";

    public const string WirelessConnectionType = "802-11-wireless";

    /// <summary>Calls a method on the main NetworkManager object. Takes ownership of the arguments.</summary>
    public static void Call(string method, params Variant[] arguments)
    {
        using var networkManager = CreateProxy(MainPath, MainInterface);

        using var parameters = Variants.Tuple(arguments);
        using var _ = networkManager.CallSync(method, parameters, DBusCallFlags.None, -1, null);
    }

    public static DBusProxy CreateProxy(string path, string interfaceName)
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
}
