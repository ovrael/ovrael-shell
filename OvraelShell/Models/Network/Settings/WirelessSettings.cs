using GLib;
using OvraelShell.Enums.Network;
using OvraelShell.Utils;

namespace OvraelShell.Models.Network.Settings;

/// <summary>The Wi-Fi part of a profile's settings.</summary>
public sealed record WirelessSettings(
    string Ssid,
    string? LockedBssid,
    IReadOnlySet<string> Bssids,
    bool NeedsPassword
)
{
    // NM_SETTING_SECRET_FLAG_AGENT_OWNED | NM_SETTING_SECRET_FLAG_NOT_SAVED
    private const uint SecretNotStoredFlags = 0x1 | 0x2;

    public static readonly IReadOnlySet<string> NoBssids = new HashSet<string>();

    /// <summary>Reads the 802-11-wireless and 802-11-wireless-security settings.</summary>
    public static WirelessSettings Read(Variant settings)
    {
        using var wireless = ConnectionSettings.Lookup(settings, ConnectionSettings.Wireless);
        using var security = ConnectionSettings.Lookup(
            settings,
            ConnectionSettings.WirelessSecurity
        );

        using var ssid = VariantGets.Lookup(wireless, "ssid", "ay");

        var lockedBssid = ReadLockedBssid(wireless);
        var bssids = ReadSeenBssids(wireless);

        if (lockedBssid is not null)
            bssids.Add(lockedBssid);

        return new WirelessSettings(
            ProxyGets.ByteString(ssid),
            lockedBssid,
            bssids,
            ReadNeedsPassword(security)
        );
    }

    /// <summary>
    /// The profile is secured with a password NetworkManager does not keep itself
    /// (psk-flags agent-owned or not-saved), so activating it would fail without asking first.
    /// </summary>
    private static bool ReadNeedsPassword(Variant? security)
    {
        var keyMgmt = VariantGets.LookupString(security, "key-mgmt");

        if (keyMgmt is not ("wpa-psk" or "sae"))
            return false;

        using var flags = VariantGets.Lookup(security, "psk-flags", "u");

        return ((flags?.GetUint32() ?? 0) & SecretNotStoredFlags) != 0;
    }

    /// <summary>The "seen-bssids" NM has connected to.</summary>
    private static HashSet<string> ReadSeenBssids(Variant? wireless)
    {
        using var seen = VariantGets.Lookup(wireless, "seen-bssids", "as");

        return VariantGets
            .Array(seen, child => child.GetString(out _))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>The "bssid" the profile is locked to, stored as raw bytes.</summary>
    private static string? ReadLockedBssid(Variant? wireless)
    {
        using var locked = VariantGets.Lookup(wireless, "bssid", "ay");

        var bytes = VariantGets.Array(locked, child => child.GetByte());

        return bytes.Count == 0 ? null : Bssid.FromBytes(bytes);
    }
}
