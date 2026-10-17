using Gio;
using GLib;
using Task = System.Threading.Tasks.Task;

namespace OvraelShell.Utils;

public static class BluezBus
{
    public const string BusName = "org.bluez";

    public const string RootPath = "/";

    public const string ObjectManagerInterface = "org.freedesktop.DBus.ObjectManager";

    public const string PropertiesInterface = "org.freedesktop.DBus.Properties";

    public const string AdapterInterface = "org.bluez.Adapter1";

    public const string DeviceInterface = "org.bluez.Device1";

    public const string BatteryInterface = "org.bluez.Battery1";

    private static DBusConnection? connection;

    private static DBusConnection Connection =>
        connection ??= Gio.Functions.BusGetSync(BusType.System, null);

    public static DBusProxy CreateProxy(
        string path,
        string interfaceName,
        DBusProxyFlags flags = DBusProxyFlags.None
    )
    {
        return DBusProxy.NewForBusSync(
            BusType.System,
            flags,
            null,
            BusName,
            path,
            interfaceName,
            null
        );
    }

    /// <summary>
    /// Calls a method without blocking the main loop - Connect and Pair take seconds.
    /// Takes ownership of the arguments. Throws <see cref="GException"/> with the BlueZ error.
    /// </summary>
    public static async Task CallAsync(
        string path,
        string interfaceName,
        string method,
        params Variant[] arguments
    )
    {
        using var parameters = Variants.Tuple(arguments);
        using var _ = await Connection.CallAsync(BusName, path, interfaceName, method, parameters);
    }

    /// <summary>Sets a writable property, e.g. Powered. Takes ownership of <paramref name="value"/>.</summary>
    public static Task SetPropertyAsync(
        string path,
        string interfaceName,
        string property,
        Variant value
    )
    {
        var boxed = Variant.NewVariant(value);
        value.Dispose();

        return CallAsync(
            path,
            PropertiesInterface,
            "Set",
            Variant.NewString(interfaceName),
            Variant.NewString(property),
            boxed
        );
    }

    /// <summary>The error for the user - without the D-Bus prefix, common ones in plain words.</summary>
    public static string ErrorMessage(GException exception)
    {
        var message = exception.Message;

        if (message.Contains("page-timeout") || message.Contains("Host is down"))
            return "The device does not respond - is it on and near?";

        if (message.Contains("org.bluez.Error.Authentication"))
            return "Pairing failed";

        if (message.Contains("org.bluez.Error.Blocked") || message.Contains("rfkill"))
            return "Bluetooth is blocked (rfkill)";

        // GDBus.Error:org.bluez.Error.Failed: text
        var separator = message.IndexOf(": ", StringComparison.Ordinal);
        return message.StartsWith("GDBus.Error:") && separator >= 0
            ? message[(separator + 2)..]
            : message;
    }
}
