using OvraelShell.Enums.Network;
using OvraelShell.Models.Network;

namespace OvraelShell.InterfaceElements;

/// <summary>Picks the icon for a network value.</summary>
public static class NetworkIcons
{
    public static string Strength(uint strength)
    {
        if (strength > 100)
            return Icons.Common.SpeedMid;

        return WirelessNetwork.StrengthLevelOf(strength) switch
        {
            0 => Icons.Network.SignalStrengthVeryLow,
            1 => Icons.Network.SignalStrengthLow,
            2 => Icons.Network.SignalStrengthMedium,
            _ => Icons.Network.SignalStrengthHigh,
        };
    }

    /// <summary>Band by frequency in MHz: 2.4 GHz, 5 GHz, 6 GHz.</summary>
    public static string Frequency(uint frequency)
    {
        return frequency switch
        {
            < 4000 => Icons.Common.SpeedSlow,
            < 5925 => Icons.Common.SpeedMid,
            _ => Icons.Common.SpeedFast,
        };
    }

    public static string Saved(bool isSaved) => isSaved ? Icons.Common.Saved : Icons.Common.Unsaved;

    public static string Privacy(bool hasPrivacy) =>
        hasPrivacy ? Icons.Common.Locked : Icons.Common.Unlocked;

    public static string Connection(bool isConnected, bool isWifi)
    {
        return (isConnected, isWifi) switch
        {
            (true, true) => Icons.Network.WifiConnected,
            (true, false) => Icons.Network.EthernetConnected,
            (false, true) => Icons.Network.WifiDisconnected,
            (false, false) => Icons.Network.EthernetDisconnected,
        };
    }

    public static string Ethernet(bool hasCarrier) =>
        hasCarrier ? Icons.Network.EthernetConnected : Icons.Network.EthernetDisconnected;
}
