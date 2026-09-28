namespace OvraelShell.Models.Network;

/// <summary>
/// BSSID as text (AA:BB:CC:DD:EE:FF), the way access points show it,
/// and as raw bytes, the way connection profiles store it.
/// </summary>
public static class Bssid
{
    public static byte[] ToBytes(string bssid) =>
        bssid.Split(':').Select(part => Convert.ToByte(part, 16)).ToArray();

    public static string FromBytes(IEnumerable<byte> bytes) =>
        string.Join(':', bytes.Select(value => value.ToString("X2")));
}
