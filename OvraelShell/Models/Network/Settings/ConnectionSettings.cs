using Gio;
using GLib;
using OvraelShell.Utils;

namespace OvraelShell.Models.Network.Settings;

/// <summary>
/// Settings of a saved profile (Settings.Connection): a{sa{sv}}, setting name -> (key -> value).
/// They are not D-Bus properties - they come from GetSettings() and are replaced with Update().
/// </summary>
public static class ConnectionSettings
{
    public const string Connection = "connection";
    public const string Wireless = NetworkManagerBus.WirelessConnectionType;
    public const string WirelessSecurity = "802-11-wireless-security";

    /// <summary>Reads the settings of the profile. The caller owns the result.</summary>
    public static Variant Get(DBusProxy proxy)
    {
        using var result = proxy.CallSync("GetSettings", null, DBusCallFlags.None, -1, null);

        return result.GetChildValue(0);
    }

    public static Variant? Lookup(Variant settings, string name) =>
        VariantGets.Lookup(settings, name, "a{sv}");

    /// <summary>
    /// Rewrites one setting of the profile through <paramref name="edit"/>, keeping the others.
    /// Secrets survive: GetSettings leaves them out, and when Update gets none,
    /// NM keeps the stored ones - otherwise it stores what it gets.
    /// Throws <see cref="GException"/> when NetworkManager rejects the update.
    /// </summary>
    public static void Update(DBusProxy proxy, string settingName, Action<SettingEntries> edit)
    {
        using var settings = Get(proxy);

        var entries = VariantGets.Entries(settings);
        var index = entries.FindIndex(entry => entry.Key == settingName);

        SettingEntries setting;

        if (index >= 0)
        {
            setting = new SettingEntries(VariantGets.Entries(entries[index].Value));
            entries[index].Value.Dispose();
            entries.RemoveAt(index);
        }
        else
        {
            setting = new SettingEntries([]);
        }

        edit(setting);

        entries.Add((settingName, setting.ToVariant()));

        using var updated = Variants.Dictionary("a{sv}", entries.ToArray());
        using var parameters = Variants.Tuple(updated);
        using var _ = proxy.CallSync("Update", parameters, DBusCallFlags.None, -1, null);
    }
}
