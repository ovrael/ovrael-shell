using GLib;

namespace OvraelShell.Utils;

public static class VariantGets
{
    /// <summary>
    /// Looks up <paramref name="key"/> in a dictionary variant (a{sv} or a{s*}) and unboxes "v" values.
    /// Returns null when the key is missing or has a different type.
    /// </summary>
    /// <remarks>
    /// Not Variant.LookupValue: GirCore 0.8.1 declares it non-nullable and wraps a NULL result
    /// in a Variant object, so the first call on it crashes with SIGSEGV.
    /// </remarks>
    public static Variant? Lookup(Variant? dictionary, string key, string typeString)
    {
        if (dictionary is null)
            return null;

        var count = dictionary.NChildren();

        for (nuint i = 0; i < count; i++)
        {
            using var entry = dictionary.GetChildValue(i);
            using var entryKey = entry.GetChildValue(0);

            if (entryKey.GetString(out _) != key)
                continue;

            var value = entry.GetChildValue(1);

            if (value.GetTypeString() == "v")
            {
                var boxed = value;
                value = boxed.GetVariant();
                boxed.Dispose();
            }

            if (value.GetTypeString() == typeString)
                return value;

            value.Dispose();
            return null;
        }

        return null;
    }

    /// <summary>A string value of a dictionary variant, or an empty string when it is missing.</summary>
    public static string LookupString(Variant? dictionary, string key)
    {
        using var value = Lookup(dictionary, key, "s");

        return value?.GetString(out _) ?? string.Empty;
    }

    /// <summary>Children of an array variant (e.g. as) read with <paramref name="read"/>. Empty for null.</summary>
    public static List<T> Array<T>(Variant? array, Func<Variant, T> read)
    {
        var items = new List<T>();

        if (array is null)
            return items;

        var count = array.NChildren();

        for (nuint i = 0; i < count; i++)
        {
            using var child = array.GetChildValue(i);
            items.Add(read(child));
        }

        return items;
    }

    /// <summary>
    /// All entries of a dictionary variant, with "v" values unboxed. The caller owns the values.
    /// </summary>
    public static List<(string Key, Variant Value)> Entries(Variant dictionary)
    {
        var entries = new List<(string, Variant)>();
        var count = dictionary.NChildren();

        for (nuint i = 0; i < count; i++)
        {
            using var entry = dictionary.GetChildValue(i);
            using var key = entry.GetChildValue(0);

            var value = entry.GetChildValue(1);

            if (value.GetTypeString() == "v")
            {
                var boxed = value;
                value = boxed.GetVariant();
                boxed.Dispose();
            }

            entries.Add((key.GetString(out _), value));
        }

        return entries;
    }
}
