using OvraelShell.Enums.Network;

namespace OvraelShell.Models.Network;

/// <summary>Device states and failure reasons as shown to the user.</summary>
public static class DeviceStateTexts
{
    public static string ToDisplayText(this DeviceState state)
    {
        return state switch
        {
            DeviceState.Activated => "Connected",
            DeviceState.Prepare
            or DeviceState.Config
            or DeviceState.IpConfig
            or DeviceState.IpCheck
            or DeviceState.Secondaries => "Connecting...",
            DeviceState.NeedAuth => "Needs authentication",
            DeviceState.Deactivating => "Disconnecting...",
            DeviceState.Failed => "Failed",
            _ => state.ToString(),
        };
    }

    public static string ToFailureText(this DeviceStateReason reason)
    {
        return reason switch
        {
            DeviceStateReason.SsidNotFound => "Network is out of range",
            DeviceStateReason.SupplicantTimeout => "Connection timed out",
            DeviceStateReason.IpConfigUnavailable or DeviceStateReason.IpConfigExpired =>
                "Could not get an IP address",
            DeviceStateReason.UserRequested => "Disconnected",
            _ => $"Connection failed ({reason})",
        };
    }
}
