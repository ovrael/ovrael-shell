namespace OvraelShell.Enums.Network;

public enum WirelessSecurity
{
    None,

    /// <summary>Enhanced Open - encrypted, but no password.</summary>
    Owe,

    /// <summary>WPA/WPA2 Personal (also WPA2/WPA3 transition mode).</summary>
    WpaPsk,

    /// <summary>WPA3 Personal only.</summary>
    Sae,

    /// <summary>WPA Enterprise (802.1X) - not supported yet.</summary>
    Enterprise,

    /// <summary>Legacy WEP - not supported.</summary>
    Wep,
}
