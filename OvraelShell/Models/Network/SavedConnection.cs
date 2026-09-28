using Gio;
using GLib;
using OvraelShell.Enums.Network;
using OvraelShell.Models.Network.Settings;
using OvraelShell.Utils;

namespace OvraelShell.Models.Network;

/// <summary>A connection profile saved in NetworkManager (Settings.Connection).</summary>
public sealed class SavedConnection : IWithObjectPath, IDisposable
{
    private readonly DBusProxy proxy;

    public string ObjectPath { get; }

    public ReactiveProperty<string> Id { get; } = new(string.Empty);
    public ReactiveProperty<string> Uuid { get; } = new(string.Empty);
    public ReactiveProperty<string> Type { get; } = new(string.Empty);

    /// <summary>Null for non Wi-Fi connections.</summary>
    public ReactiveProperty<string?> Ssid { get; } = new();

    /// <summary>
    /// BSSIDs (AA:BB:CC:DD:EE:FF) this profile belongs to: the locked "bssid"
    /// plus the "seen-bssids" NM has connected to. Empty for non Wi-Fi connections.
    /// </summary>
    public ReactiveProperty<IReadOnlySet<string>> Bssids { get; } = new(WirelessSettings.NoBssids);

    /// <summary>The "bssid" the profile is locked to - NM refuses it on any other access point.</summary>
    public ReactiveProperty<string?> LockedBssid { get; } = new();

    /// <inheritdoc cref="WirelessSettings.NeedsPassword"/>
    public ReactiveProperty<bool> NeedsPassword { get; } = new();

    public bool IsWireless => Type.Value == ConnectionSettings.Wireless;

    private SavedConnection(string objectPath, DBusProxy proxy)
    {
        ObjectPath = objectPath;
        this.proxy = proxy;

        // NM emits "Updated" when the settings change
        proxy.OnGSignal += (_, args) =>
        {
            if (args.SignalName == "Updated")
                UpdateSettings();
        };

        UpdateSettings();
    }

    public static SavedConnection Create(string objectPath)
    {
        var proxy = NetworkManagerBus.CreateProxy(
            objectPath,
            NetworkManagerBus.SettingsConnectionInterface
        );

        return new SavedConnection(objectPath, proxy);
    }

    private void UpdateSettings()
    {
        try
        {
            using var settings = ConnectionSettings.Get(proxy);

            using var connection = ConnectionSettings.Lookup(
                settings,
                ConnectionSettings.Connection
            );

            Id.Set(VariantGets.LookupString(connection, "id"));
            Uuid.Set(VariantGets.LookupString(connection, "uuid"));
            Type.Set(VariantGets.LookupString(connection, "type"));

            SetWireless(IsWireless ? WirelessSettings.Read(settings) : null);
        }
        catch (GException ex)
        {
            System.Console.WriteLine($"Cannot read settings of {ObjectPath}: {ex.Message}");
        }
    }

    private void SetWireless(WirelessSettings? wireless)
    {
        Ssid.Set(wireless?.Ssid);
        LockedBssid.Set(wireless?.LockedBssid);
        NeedsPassword.Set(wireless?.NeedsPassword ?? false);

        var bssids = wireless?.Bssids ?? WirelessSettings.NoBssids;

        // ReactiveProperty compares by reference - only notify on a real change
        if (!Bssids.Value.SetEquals(bssids))
            Bssids.Set(bssids);
    }

    /// <summary>
    /// Replaces the stored Wi-Fi password, keeping the rest of the profile.
    /// <paramref name="keyMgmt"/> is used only when the profile has no security setting yet.
    /// Throws <see cref="GException"/> when NetworkManager rejects the update.
    /// </summary>
    public void SetPassword(string keyMgmt, string password)
    {
        ConnectionSettings.Update(
            proxy,
            ConnectionSettings.WirelessSecurity,
            security =>
            {
                security.Set("psk", Variant.NewString(password));

                // Let NM store the password, so the next Connect does not have to ask again
                security.Set("psk-flags", Variant.NewUint32(0));

                // Keep the profile's own key-mgmt (e.g. sae) - only fill it in when missing
                if (!security.Contains("key-mgmt"))
                    security.Set("key-mgmt", Variant.NewString(keyMgmt));
            }
        );
    }

    /// <summary>
    /// Locks the profile to <paramref name="bssid"/> (AA:BB:CC:DD:EE:FF), or unlocks it with null.
    /// Throws <see cref="GException"/> when NetworkManager rejects the update.
    /// </summary>
    public void SetLockedBssid(string? bssid)
    {
        ConnectionSettings.Update(
            proxy,
            ConnectionSettings.Wireless,
            wireless =>
            {
                if (bssid is null)
                    wireless.Remove("bssid");
                else
                    wireless.Set("bssid", Variants.ByteArray(Bssid.ToBytes(bssid)));
            }
        );
    }

    /// <summary>
    /// Whether the profile can be activated on <paramref name="network"/>: same SSID, and if the
    /// profile is locked to a BSSID, that one. SSID matters - a router can keep its BSSIDs
    /// after being renamed, leaving old profiles that point at the same access points.
    /// </summary>
    public bool Matches(WirelessNetwork network) =>
        IsWireless
        && Ssid.Value == network.Ssid
        && (
            LockedBssid.Value is null
            || string.Equals(LockedBssid.Value, network.Bssid, StringComparison.OrdinalIgnoreCase)
        );

    public void Dispose()
    {
        proxy.Dispose();
    }
}
