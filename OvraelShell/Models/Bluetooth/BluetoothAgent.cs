using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using GLib;
using OvraelShell.Utils;
using Task = System.Threading.Tasks.Task;

namespace OvraelShell.Models.Bluetooth;

/// <summary>
/// The BlueZ pairing agent (org.bluez.Agent1). Without an agent BlueZ cannot ask to confirm
/// a pairing, so the pairing fails. Accepts only the devices that the shell pairs now
/// (<see cref="AllowPairing"/>) and rejects all other requests. There is no UI for
/// a passkey, so the capability is NoInputNoOutput (Just Works).
/// </summary>
/// <remarks>
/// Exported through GIO directly - GirCore 0.8.1 does not give a public way to make
/// the closures for g_dbus_connection_register_object_with_closures.
/// </remarks>
internal static partial class BluetoothAgent
{
    public const string ObjectPath = "/org/ovraelshell/BluetoothAgent";

    private const string AgentManagerInterface = "org.bluez.AgentManager1";
    private const string AgentInterface = "org.bluez.Agent1";
    private const string Capability = "NoInputNoOutput";

    // For devices with a fixed legacy PIN (before Bluetooth 2.1)
    private const string DefaultPinCode = "0000";

    private const string Introspection = """
        <node>
          <interface name="org.bluez.Agent1">
            <method name="Release"/>
            <method name="RequestPinCode">
              <arg type="o" direction="in"/>
              <arg type="s" direction="out"/>
            </method>
            <method name="DisplayPinCode">
              <arg type="o" direction="in"/>
              <arg type="s" direction="in"/>
            </method>
            <method name="RequestPasskey">
              <arg type="o" direction="in"/>
              <arg type="u" direction="out"/>
            </method>
            <method name="DisplayPasskey">
              <arg type="o" direction="in"/>
              <arg type="u" direction="in"/>
              <arg type="q" direction="in"/>
            </method>
            <method name="RequestConfirmation">
              <arg type="o" direction="in"/>
              <arg type="u" direction="in"/>
            </method>
            <method name="RequestAuthorization">
              <arg type="o" direction="in"/>
            </method>
            <method name="AuthorizeService">
              <arg type="o" direction="in"/>
              <arg type="s" direction="in"/>
            </method>
            <method name="Cancel"/>
          </interface>
        </node>
        """;

    private const string GioLibrary = "libgio-2.0.so.0";
    private const string GLibLibrary = "libglib-2.0.so.0";

    // G_BUS_TYPE_SYSTEM
    private const int SystemBus = 1;

    // method_call, get_property, set_property and gpointer padding[8]
    private const int VTableSlots = 11;

    // Device paths with a pairing started by the shell - counted, the same device can be paired twice
    private static readonly Dictionary<string, int> allowedDevices = [];

    private static bool exported;

    /// <summary>
    /// Puts the agent object on the system bus. Call once, on the GLib main thread -
    /// the agent methods are called on it.
    /// </summary>
    public static unsafe void Export()
    {
        if (exported)
            return;

        IntPtr error = IntPtr.Zero;

        // The shared system bus connection - the same one GirCore uses. Kept for the whole run.
        var connection = g_bus_get_sync(SystemBus, IntPtr.Zero, &error);
        if (connection == IntPtr.Zero)
            throw new Exception($"Cannot connect to the system bus: {TakeError(error)}");

        // Kept for the whole run - the registration uses the interface info
        var node = g_dbus_node_info_new_for_xml(Introspection, &error);
        if (node == IntPtr.Zero)
            throw new Exception($"Bad agent introspection: {TakeError(error)}");

        var interfaceInfo = g_dbus_node_info_lookup_interface(node, AgentInterface);

        var vtable = (IntPtr*)NativeMemory.AllocZeroed(VTableSlots, (nuint)sizeof(IntPtr));
        vtable[0] = (IntPtr)(
            delegate* unmanaged[Cdecl]<
                IntPtr,
                IntPtr,
                IntPtr,
                IntPtr,
                IntPtr,
                IntPtr,
                IntPtr,
                IntPtr,
                void>)&OnMethodCall;

        var id = g_dbus_connection_register_object(
            connection,
            ObjectPath,
            interfaceInfo,
            (IntPtr)vtable,
            IntPtr.Zero,
            IntPtr.Zero,
            &error
        );

        if (id == 0)
            throw new Exception($"Cannot export the Bluetooth agent: {TakeError(error)}");

        exported = true;
    }

    /// <summary>
    /// Registers the agent with BlueZ as the default one. Call after each start of bluetoothd -
    /// BlueZ forgets the agents when it stops.
    /// </summary>
    public static async Task RegisterAsync()
    {
        try
        {
            await BluezBus.CallAsync(
                "/org/bluez",
                AgentManagerInterface,
                "RegisterAgent",
                Variant.NewObjectPath(ObjectPath),
                Variant.NewString(Capability)
            );

            await BluezBus.CallAsync(
                "/org/bluez",
                AgentManagerInterface,
                "RequestDefaultAgent",
                Variant.NewObjectPath(ObjectPath)
            );
        }
        catch (GException exception)
            when (exception.Message.Contains("org.bluez.Error.AlreadyExists"))
        {
            // Registered before - this bluetoothd knows the agent
        }
        catch (GException exception)
        {
            Console.WriteLine($"Bluetooth: cannot register the pairing agent: {exception.Message}");
        }
    }

    /// <summary>
    /// Accepts the pairing requests of the device until the result is disposed.
    /// </summary>
    public static IDisposable AllowPairing(string devicePath)
    {
        allowedDevices[devicePath] = allowedDevices.GetValueOrDefault(devicePath) + 1;
        return new Allowance(devicePath);
    }

    private sealed class Allowance(string devicePath) : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            if (allowedDevices.GetValueOrDefault(devicePath) <= 1)
                allowedDevices.Remove(devicePath);
            else
                allowedDevices[devicePath]--;
        }
    }

    #region Agent methods

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnMethodCall(
        IntPtr connection,
        IntPtr sender,
        IntPtr objectPath,
        IntPtr interfaceName,
        IntPtr methodName,
        IntPtr parameters,
        IntPtr invocation,
        IntPtr userData
    )
    {
        // The invocation must get exactly one answer - BlueZ waits for it
        try
        {
            HandleMethodCall(Marshal.PtrToStringUTF8(methodName) ?? "", parameters, invocation);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Bluetooth agent failed: {exception}");
            Reject(invocation, "The agent failed");
        }
    }

    private static void HandleMethodCall(string method, IntPtr parameters, IntPtr invocation)
    {
        switch (method)
        {
            case "Release":
            case "Cancel":
                Accept(invocation);
                return;

            case "DisplayPinCode":
            case "DisplayPasskey":
                // NoInputNoOutput - BlueZ does not ask to show a code
                Accept(invocation);
                return;
        }

        // All other methods have the device as the first argument
        var device = DeviceArgument(parameters);

        if (!allowedDevices.ContainsKey(device))
        {
            Console.WriteLine($"Bluetooth agent: rejected {method} for {device}");
            Reject(invocation, "The shell did not start this pairing");
            return;
        }

        switch (method)
        {
            case "RequestPinCode":
                ReturnSingle(invocation, g_variant_new_string(DefaultPinCode));
                break;

            case "RequestPasskey":
                ReturnSingle(invocation, g_variant_new_uint32(0));
                break;

            case "RequestConfirmation":
            case "RequestAuthorization":
            case "AuthorizeService":
                Accept(invocation);
                break;

            default:
                g_dbus_method_invocation_return_dbus_error(
                    invocation,
                    "org.freedesktop.DBus.Error.UnknownMethod",
                    $"Unknown method {method}"
                );
                break;
        }
    }

    private static string DeviceArgument(IntPtr parameters)
    {
        var child = g_variant_get_child_value(parameters, 0);
        try
        {
            return Marshal.PtrToStringUTF8(g_variant_get_string(child, IntPtr.Zero)) ?? "";
        }
        finally
        {
            g_variant_unref(child);
        }
    }

    // The return_* calls take the invocation and a floating variant
    private static void Accept(IntPtr invocation) =>
        g_dbus_method_invocation_return_value(invocation, IntPtr.Zero);

    private static unsafe void ReturnSingle(IntPtr invocation, IntPtr value) =>
        g_dbus_method_invocation_return_value(invocation, g_variant_new_tuple(&value, 1));

    private static void Reject(IntPtr invocation, string message) =>
        g_dbus_method_invocation_return_dbus_error(invocation, "org.bluez.Error.Rejected", message);

    #endregion

    #region Native

    // GError { GQuark domain; gint code; gchar *message; }
    private static string TakeError(IntPtr error)
    {
        if (error == IntPtr.Zero)
            return "unknown error";

        var message = Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(error, 8)) ?? "unknown error";
        g_error_free(error);
        return message;
    }

    [LibraryImport(GioLibrary)]
    private static unsafe partial IntPtr g_bus_get_sync(int busType, IntPtr cancellable, IntPtr* error);

    [LibraryImport(GioLibrary, StringMarshalling = StringMarshalling.Utf8)]
    private static unsafe partial IntPtr g_dbus_node_info_new_for_xml(string xml, IntPtr* error);

    [LibraryImport(GioLibrary, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr g_dbus_node_info_lookup_interface(IntPtr node, string name);

    [LibraryImport(GioLibrary, StringMarshalling = StringMarshalling.Utf8)]
    private static unsafe partial uint g_dbus_connection_register_object(
        IntPtr connection,
        string objectPath,
        IntPtr interfaceInfo,
        IntPtr vtable,
        IntPtr userData,
        IntPtr userDataFreeFunc,
        IntPtr* error
    );

    [LibraryImport(GioLibrary)]
    private static partial void g_dbus_method_invocation_return_value(
        IntPtr invocation,
        IntPtr parameters
    );

    [LibraryImport(GioLibrary, StringMarshalling = StringMarshalling.Utf8)]
    private static partial void g_dbus_method_invocation_return_dbus_error(
        IntPtr invocation,
        string errorName,
        string errorMessage
    );

    [LibraryImport(GLibLibrary)]
    private static partial IntPtr g_variant_get_child_value(IntPtr value, nuint index);

    [LibraryImport(GLibLibrary)]
    private static partial IntPtr g_variant_get_string(IntPtr value, IntPtr length);

    [LibraryImport(GLibLibrary, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr g_variant_new_string(string value);

    [LibraryImport(GLibLibrary)]
    private static partial IntPtr g_variant_new_uint32(uint value);

    [LibraryImport(GLibLibrary)]
    private static unsafe partial IntPtr g_variant_new_tuple(IntPtr* children, nuint count);

    [LibraryImport(GLibLibrary)]
    private static partial void g_variant_unref(IntPtr value);

    [LibraryImport(GLibLibrary)]
    private static partial void g_error_free(IntPtr error);

    #endregion
}
