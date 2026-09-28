using Gio;
using GLib;
using OvraelShell.Enums.Network;
using OvraelShell.Models;
using OvraelShell.Models.Network;
using OvraelShell.Utils;

public sealed class WirelessDevice : NetworkDevice
{
    private readonly DBusProxy wirelessProxy;
    private readonly SavedConnectionStore savedConnections;
    private readonly WirelessConnector connector;
    private readonly ConnectProgress progress;

    public ReactiveCollection<WirelessNetwork> AvailableNetworks { get; } = new();
    public ReactiveProperty<WirelessNetwork?> ActiveNetwork { get; } = new();

    // The current or last active network and the one before it - kept on the list when NM
    // drops them, see SyncNetworks. The new one can become active before the old one is dropped.
    private WirelessNetwork? lastActive;
    private WirelessNetwork? previousActive;
    private readonly List<WirelessNetwork> retained = [];

    public WirelessDevice(string objectPath, DBusProxy proxy, SavedConnectionStore savedConnections)
        : base(objectPath, proxy)
    {
        this.savedConnections = savedConnections;
        savedConnections.Changed += MarkSavedNetworks;

        connector = new WirelessConnector(this, savedConnections);
        progress = new ConnectProgress(this);

        wirelessProxy = NetworkManagerBus.CreateProxy(
            objectPath,
            NetworkManagerBus.WirelessInterface
        );

        // Order matters: ActiveAccessPoint looks up the network in AvailableNetworks
        ProxyGets.Bind(
            wirelessProxy,
            UpdateProperty,
            "AccessPoints",
            "ActiveAccessPoint",
            "LastScan"
        );
    }

    public void RequestScan()
    {
        // No options - a{sv}
        using var options = Variants.Dictionary("v");
        using var parameters = Variant.NewTuple([options]);

        wirelessProxy.CallSync("RequestScan", parameters, DBusCallFlags.None, -1, null);
    }

    /// <summary>
    /// Connects to <paramref name="network"/>: activates its saved profile, or creates a new one.
    /// With a <paramref name="password"/> and a saved profile, the profile's password is replaced first.
    /// Progress and failures are reported through <see cref="WirelessNetwork.ConnectStatus"/>
    /// and <see cref="WirelessNetwork.PasswordRejected"/>.
    /// Throws <see cref="GException"/> when NetworkManager rejects the request
    /// and <see cref="NotSupportedException"/> for unsupported security.
    /// </summary>
    public ConnectResult Connect(WirelessNetwork network, string? password = null)
    {
        try
        {
            var result = connector.Connect(network, password);
            progress.Report(network, result);

            return result;
        }
        catch (Exception ex) when (ex is GException or NotSupportedException)
        {
            network.ConnectStatus.Set(ex.Message);
            throw;
        }
    }

    private void UpdateProperty(string property)
    {
        switch (property)
        {
            case "AccessPoints":
                SyncNetworks(ProxyGets.ObjectPathArray(wirelessProxy, property));
                UpdateActiveNetwork();
                break;

            case "ActiveAccessPoint":
                UpdateActiveNetwork();
                break;

            // Scans during an activation look only for the target access point
            case "LastScan" when !IsActivating:
                if (retained.Count > 0)
                {
                    foreach (var network in retained.ToArray())
                        DropRetained(network);

                    UpdateActiveNetwork();
                }
                break;
        }
    }

    /// <summary>
    /// Disconnecting from an access point (e.g. switching to the other band) changes the MAC
    /// address for scanning, which flushes the scan results - NM drops the access point until
    /// the next scan. So the last active networks stay on the list as
    /// <see cref="WirelessNetwork.IsRetained"/> until found again (under a new path)
    /// or until a scan ends without them.
    /// </summary>
    private void SyncNetworks(string[] paths)
    {
        foreach (var left in new[] { lastActive, previousActive })
        {
            if (
                left is not null
                && !left.IsRetained
                && !paths.Contains(left.ObjectPath)
                && AvailableNetworks.Items.Contains(left)
            )
            {
                left.IsRetained = true;
                retained.Add(left);
            }
        }

        AvailableNetworks.Sync(
            [.. paths, .. retained.Select(network => network.ObjectPath)],
            network => network.ObjectPath,
            CreateNetwork,
            network => network.Release()
        );

        foreach (var network in retained.ToArray())
        {
            var found = AvailableNetworks.Items.FirstOrDefault(other =>
                !other.IsRetained && other.Bssid == network.Bssid && other.Ssid == network.Ssid
            );

            if (found is null)
                continue;

            progress.Transfer(network, found);
            DropRetained(network);
        }
    }

    private void DropRetained(WirelessNetwork network)
    {
        retained.Remove(network);

        if (lastActive == network)
            lastActive = null;

        if (previousActive == network)
            previousActive = null;

        AvailableNetworks.Remove(network);
        network.Release();
    }

    private bool IsActivating => State.Value is >= DeviceState.Prepare and <= DeviceState.Secondaries;

    private WirelessNetwork? CreateNetwork(string path)
    {
        var network = WirelessNetwork.Create(path);

        if (network is not null)
            network.IsSaved.Set(savedConnections.IsSaved(network));

        return network;
    }

    private void MarkSavedNetworks()
    {
        foreach (var network in AvailableNetworks.Items)
            network.IsSaved.Set(savedConnections.IsSaved(network));
    }

    private void UpdateActiveNetwork()
    {
        var path = ProxyGets.ObjectPath(wirelessProxy, "ActiveAccessPoint");

        ActiveNetwork.Set(
            path is null ? null : AvailableNetworks.Items.FirstOrDefault(n => n.ObjectPath == path)
        );

        if (ActiveNetwork.Value is { } active && active != lastActive)
        {
            previousActive = lastActive;
            lastActive = active;
        }
    }

    public override void Dispose()
    {
        // The store outlives the device
        savedConnections.Changed -= MarkSavedNetworks;

        foreach (var network in AvailableNetworks.Items)
            network.Release();

        ActiveNetwork.Set(null);
        lastActive = null;
        previousActive = null;
        retained.Clear();
        AvailableNetworks.Clear();

        wirelessProxy.Dispose();
        base.Dispose();
    }
}
