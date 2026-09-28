using Gio;
using OvraelShell.Utils;

namespace OvraelShell.Models.Network;

/// <summary>Connection profiles saved in NetworkManager (org.freedesktop.NetworkManager.Settings).</summary>
public sealed class SavedConnectionStore : IDisposable
{
    private readonly DBusProxy proxy;

    public ReactiveCollection<SavedConnection> Connections { get; } = new();

    /// <summary>Raised when the list changes or the SSID / BSSIDs of a saved profile change.</summary>
    public event System.Action? Changed;

    public SavedConnectionStore()
    {
        proxy = NetworkManagerBus.CreateProxy(
            NetworkManagerBus.SettingsPath,
            NetworkManagerBus.SettingsInterface
        );

        proxy.OnGSignal += (_, args) =>
        {
            if (args.SignalName is "NewConnection" or "ConnectionRemoved")
                Update();
        };

        Connections.Changed += () => Changed?.Invoke();

        Update();
    }

    /// <summary>A matching profile (same SSID) has already been used on this access point.</summary>
    public bool IsSaved(WirelessNetwork network) =>
        Connections.Items.Any(connection =>
            connection.Matches(network) && connection.Bssids.Value.Contains(network.Bssid)
        );

    /// <summary>
    /// Profile to activate for <paramref name="network"/>: one already used on this access point
    /// first, then any other with the same SSID, so a new access point of a known network
    /// reuses its profile.
    /// </summary>
    public SavedConnection? FindFor(WirelessNetwork network)
    {
        var matching = Connections.Items.Where(connection => connection.Matches(network)).ToList();

        return matching.FirstOrDefault(connection =>
                connection.Bssids.Value.Contains(network.Bssid)
            ) ?? matching.FirstOrDefault();
    }

    private void Update()
    {
        string[] paths;

        try
        {
            using var result = proxy.CallSync("ListConnections", null, DBusCallFlags.None, -1, null);
            using var connections = result.GetChildValue(0);

            paths = connections.GetObjv(out _);
        }
        catch (GLib.GException ex)
        {
            System.Console.WriteLine($"Cannot list saved connections: {ex.Message}");
            return;
        }

        Connections.Sync(
            paths,
            connection => connection.ObjectPath,
            CreateConnection,
            connection => connection.Dispose()
        );
    }

    private SavedConnection CreateConnection(string path)
    {
        var connection = SavedConnection.Create(path);

        // NM adds to seen-bssids after connecting to a new access point
        connection.Bssids.Changed += (_) => Changed?.Invoke();

        // Matching depends on the SSID too - it can be edited in an existing profile
        connection.Ssid.Changed += (_) => Changed?.Invoke();

        return connection;
    }

    public void Dispose()
    {
        foreach (var connection in Connections.Items)
            connection.Dispose();

        Connections.Clear();

        proxy.Dispose();
    }
}
