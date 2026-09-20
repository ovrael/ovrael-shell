namespace OvraelShell.Models;

public sealed record WifiNetwork(
    string Ssid,
    string Bssid,
    byte Strength,
    uint Frequency,
    uint Flags,
    uint WpaFlags,
    uint RsnFlags,
    string ObjectPath
)
{
    public override string ToString() =>
        $"WifiNetwork {{ Ssid = {Ssid}, Bssid = {Bssid}, Strength = {Strength}%, "
        + $"Frequency = {Frequency} MHz, Flags = {Flags}, WpaFlags = {WpaFlags}, "
        + $"RsnFlags = {RsnFlags}, ObjectPath = {ObjectPath} }}";
};
