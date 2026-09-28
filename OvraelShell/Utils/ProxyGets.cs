using System.Text;
using Gio;
using GLib;

namespace OvraelShell.Utils;

public static class ProxyGets
{
    /// <summary>
    /// Calls <paramref name="update"/> for each of the given properties right away,
    /// and then every time one of them changes on D-Bus.
    /// </summary>
    public static void Bind(DBusProxy proxy, Action<string> update, params string[] properties)
    {
        foreach (var property in properties)
            update(property);

        proxy.OnGPropertiesChanged += (_, args) =>
        {
            foreach (var property in ChangedProperties(args.ChangedProperties))
            {
                if (properties.Contains(property))
                    update(property);
            }
        };
    }

    public static IEnumerable<string> ChangedProperties(Variant properties)
    {
        var count = properties.NChildren();

        for (nuint i = 0; i < count; i++)
        {
            using var entry = properties.GetChildValue(i);
            using var key = entry.GetChildValue(0);
            yield return key.GetString(out _);
        }
    }

    public static uint UInt(DBusProxy proxy, string property)
    {
        var variant = proxy.GetCachedProperty(property);

        if (variant is null)
            return 0;

        return variant.GetUint32();
    }

    /// <summary>Reads a uint field of a struct property, e.g. StateReason (uu).</summary>
    public static uint UIntAt(DBusProxy proxy, string property, nuint index)
    {
        var variant = proxy.GetCachedProperty(property);

        if (variant is null || variant.NChildren() <= index)
            return 0;

        using var child = variant.GetChildValue(index);

        return child.GetUint32();
    }

    public static byte Byte(DBusProxy proxy, string property)
    {
        var variant = proxy.GetCachedProperty(property);

        if (variant is null)
            return 0;

        return variant.GetByte();
    }

    public static string? String(DBusProxy proxy, string property)
    {
        var variant = proxy.GetCachedProperty(property);

        if (variant is null)
            return null;

        return variant.GetString(out _);
    }

    public static bool? Bool(DBusProxy proxy, string property)
    {
        var variant = proxy.GetCachedProperty(property);

        if (variant is null)
            return null;

        return variant.GetBoolean();
    }

    /// <summary>Also returns null for the empty path "/".</summary>
    public static string? ObjectPath(DBusProxy proxy, string property)
    {
        var variant = proxy.GetCachedProperty(property);

        if (variant is null)
            return null;

        return NormalizePath(variant.GetString(out _));
    }

    /// <summary>Skips empty "/" paths.</summary>
    public static string[] ObjectPathArray(DBusProxy proxy, string property)
    {
        var variant = proxy.GetCachedProperty(property);

        if (variant is null)
            return [];

        return variant.GetObjv(out _).Where(path => NormalizePath(path) is not null).ToArray();
    }

    /// <summary>Reads a byte array (e.g. Ssid) as UTF-8 text.</summary>
    public static string ByteString(DBusProxy proxy, string property) =>
        ByteString(proxy.GetCachedProperty(property));

    /// <summary>Reads a byte array variant (e.g. Ssid) as UTF-8 text.</summary>
    public static string ByteString(Variant? variant)
    {
        // For "ay", NChildren is the byte count. An empty array (e.g. a hidden network) crashes
        // GetDataAsBytes in GirCore - GC.AddMemoryPressure(0) throws.
        if (variant is null || variant.NChildren() == 0)
            return string.Empty;

        try
        {
            using var bytes = variant.GetDataAsBytes();
            var data = bytes.GetRegionSpan<byte>(0, bytes.GetSize());
            return Encoding.UTF8.GetString(data);
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine(ex);
            return string.Empty;
        }
    }

    private static string? NormalizePath(string? path) =>
        string.IsNullOrEmpty(path) || path == "/" ? null : path;
}
