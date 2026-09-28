namespace OvraelShell.Enums.Network;

/// <summary>NMDeviceStateReason - only the values the UI reacts to or describes.</summary>
public enum DeviceStateReason : uint
{
    None = 0,
    Unknown = 1,
    ConfigFailed = 4,
    IpConfigUnavailable = 5,
    IpConfigExpired = 6,
    NoSecrets = 7,
    SupplicantDisconnect = 8,
    SupplicantConfigFailed = 9,
    SupplicantFailed = 10,
    SupplicantTimeout = 11,
    UserRequested = 39,
    SsidNotFound = 53,
    NewActivation = 60,
}
