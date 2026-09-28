using GLib;

namespace OvraelShell.Utils;

/// <summary>
/// Builds D-Bus argument variants. Every method takes ownership of the variants passed in
/// and disposes them once they are copied into the container.
/// </summary>
public static class Variants
{
    /// <summary>Builds a{s&lt;valueType&gt;}. For "v", values are boxed automatically.</summary>
    public static Variant Dictionary(string valueType, params (string Key, Variant Value)[] entries)
    {
        using var entryType = VariantType.New($"{{s{valueType}}}");

        var children = new List<Variant>(entries.Length);

        try
        {
            foreach (var (key, value) in entries)
            {
                using var keyVariant = Variant.NewString(key);
                using var boxed = valueType == "v" ? Variant.NewVariant(value) : null;

                children.Add(Variant.NewDictEntry(keyVariant, boxed ?? value));
            }

            return Variant.NewArray(entryType, children.ToArray());
        }
        finally
        {
            foreach (var child in children)
                child.Dispose();

            foreach (var (_, value) in entries)
                value.Dispose();
        }
    }

    /// <summary>Builds ay, e.g. a BSSID in a connection profile.</summary>
    public static Variant ByteArray(byte[] bytes)
    {
        using var byteType = VariantType.New("y");

        var children = bytes.Select(Variant.NewByte).ToArray();

        try
        {
            return Variant.NewArray(byteType, children);
        }
        finally
        {
            foreach (var child in children)
                child.Dispose();
        }
    }

    public static Variant Tuple(params Variant[] children)
    {
        try
        {
            return Variant.NewTuple(children);
        }
        finally
        {
            foreach (var child in children)
                child.Dispose();
        }
    }
}
