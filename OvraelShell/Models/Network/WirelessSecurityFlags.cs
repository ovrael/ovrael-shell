using OvraelShell.Enums.Network;

namespace OvraelShell.Models.Network;

/// <summary>Security of an access point, read from its Flags, WpaFlags and RsnFlags.</summary>
public static class WirelessSecurityFlags
{
    // NM_802_11_AP_FLAGS_PRIVACY
    private const uint FlagPrivacy = 0x1;

    // NM_802_11_AP_SEC_KEY_MGMT_* (WpaFlags / RsnFlags)
    private const uint KeyMgmtPsk = 0x100;
    private const uint KeyMgmt8021X = 0x200;
    private const uint KeyMgmtSae = 0x400;
    private const uint KeyMgmtOwe = 0x800;

    /// <summary>The access point encrypts traffic (any security but open).</summary>
    public static bool HasPrivacy(uint flags) => (flags & FlagPrivacy) != 0;

    public static WirelessSecurity Decode(uint flags, uint wpaFlags, uint rsnFlags)
    {
        var keyMgmt = wpaFlags | rsnFlags;

        if ((keyMgmt & KeyMgmt8021X) != 0)
            return WirelessSecurity.Enterprise;

        // PSK before SAE, so WPA2/WPA3 transition networks use wpa-psk
        if ((keyMgmt & KeyMgmtPsk) != 0)
            return WirelessSecurity.WpaPsk;

        if ((keyMgmt & KeyMgmtSae) != 0)
            return WirelessSecurity.Sae;

        if ((keyMgmt & KeyMgmtOwe) != 0)
            return WirelessSecurity.Owe;

        if (HasPrivacy(flags))
            return WirelessSecurity.Wep;

        return WirelessSecurity.None;
    }

    public static bool NeedsPassword(this WirelessSecurity security) =>
        security is WirelessSecurity.WpaPsk or WirelessSecurity.Sae;

    /// <summary>
    /// key-mgmt of a new profile, null for an open network.
    /// Throws <see cref="NotSupportedException"/> for Enterprise and WEP.
    /// </summary>
    public static string? KeyMgmt(this WirelessSecurity security)
    {
        return security switch
        {
            WirelessSecurity.None => null,
            WirelessSecurity.Owe => "owe",
            WirelessSecurity.WpaPsk => "wpa-psk",
            WirelessSecurity.Sae => "sae",
            _ => throw new NotSupportedException($"{security} networks are not supported"),
        };
    }
}
