using GLib;
using OvraelShell.Utils;

namespace OvraelShell.Models.Network.Settings;

/// <summary>Keys of one profile setting (e.g. 802-11-wireless) being edited. Owns the values.</summary>
public sealed class SettingEntries
{
    private readonly List<(string Key, Variant Value)> entries;

    public SettingEntries(IEnumerable<(string Key, Variant Value)> entries)
    {
        this.entries = entries.ToList();
    }

    public bool Contains(string key) => entries.Any(entry => entry.Key == key);

    public void Set(string key, Variant value)
    {
        Remove(key);
        entries.Add((key, value));
    }

    public void Remove(params string[] keys)
    {
        foreach (var (_, value) in entries.Where(entry => keys.Contains(entry.Key)))
            value.Dispose();

        entries.RemoveAll(entry => keys.Contains(entry.Key));
    }

    /// <summary>Builds the a{sv} dictionary - the values move into it.</summary>
    public Variant ToVariant()
    {
        var dictionary = Variants.Dictionary("v", entries.ToArray());
        entries.Clear();

        return dictionary;
    }
}
