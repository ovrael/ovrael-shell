using GLib;
using OvraelShell.Enums.Network;
using OvraelShell.Models.Network;
using OvraelShell.Models.Network.Settings;
using OvraelShell.Utils;

/// <summary>Sends connect requests of a Wi-Fi device: activates a saved profile or creates a new one.</summary>
public sealed class WirelessConnector
{
    private readonly WirelessDevice device;
    private readonly SavedConnectionStore savedConnections;

    // Profile locked to one access point for the activation in progress - see Activate
    private SavedConnection? lockedForActivation;

    public WirelessConnector(WirelessDevice device, SavedConnectionStore savedConnections)
    {
        this.device = device;
        this.savedConnections = savedConnections;

        device.State.Changed += OnStateChanged;
    }

    /// <inheritdoc cref="WirelessDevice.Connect"/>
    public ConnectResult Connect(WirelessNetwork network, string? password)
    {
        // A lock left by the previous attempt would pin this one to its access point
        ReleaseLock();

        var saved = savedConnections.FindFor(network);

        // Ask before touching NM - activating disconnects the current network first,
        // so a doomed attempt would drop the connection for nothing.
        // Decided by IsSaved (what the row shows), not by FindFor: FindFor also matches
        // by SSID alone, e.g. the other band of the current network.
        var needsPassword = network.IsSaved.Value
            ? saved?.NeedsPassword.Value ?? false
            : network.Security.NeedsPassword();

        if (needsPassword && string.IsNullOrEmpty(password))
            return ConnectResult.PasswordRequired;

        if (saved is null)
        {
            AddAndActivate(network, needsPassword ? password : null);
            return ConnectResult.Started;
        }

        // The access point in use - re-activating would only drop the connection and reconnect.
        // Another access point of the same profile (e.g. the other band) is activated below.
        if (string.IsNullOrEmpty(password) && IsActive(saved, network))
            return ConnectResult.AlreadyConnected;

        if (!string.IsNullOrEmpty(password))
            saved.SetPassword(network.Security.KeyMgmt() ?? "wpa-psk", password);

        Activate(saved, network);

        return ConnectResult.Started;
    }

    private bool IsActive(SavedConnection saved, WirelessNetwork network) =>
        device.ActiveNetwork.Value == network && device.IsActiveProfile(saved);

    /// <summary>
    /// Activates <paramref name="saved"/> on exactly <paramref name="network"/>. The specific object
    /// alone does not pin the access point - without a locked BSSID wpa_supplicant picks the
    /// strongest one with the SSID, usually the band already in use. So when other access points
    /// of the network are in range, the profile is locked to this one for the activation.
    /// It stays locked until the activation has its secrets: NM fetches them from the profile
    /// in the need-auth stage and refuses ("modified since activation", no-secrets) when the
    /// profile no longer matches the activated copy. Afterwards it is unlocked for autoconnect.
    /// </summary>
    private void Activate(SavedConnection saved, WirelessNetwork network)
    {
        // Without a specific object nothing else points NM at this access point
        var lockBssid =
            saved.LockedBssid.Value is null
            && (network.IsRetained || HasOtherAccessPoints(network));

        if (lockBssid)
        {
            saved.SetLockedBssid(network.Bssid);
            lockedForActivation = saved;
        }

        try
        {
            NetworkManagerBus.Call(
                "ActivateConnection",
                Variant.NewObjectPath(saved.ObjectPath),
                Variant.NewObjectPath(device.ObjectPath),
                Variant.NewObjectPath(SpecificObject(network))
            );
        }
        catch
        {
            // No activation to wait for
            ReleaseLock();
            throw;
        }
    }

    private void OnStateChanged(DeviceState state)
    {
        if (lockedForActivation is null)
            return;

        var secretsDone = state switch
        {
            // Past need-auth - the activation has its secrets
            >= DeviceState.IpConfig and <= DeviceState.Activated => true,

            DeviceState.Failed => true,

            // Except the previous connection going down before the activation starts
            DeviceState.Disconnected => device.StateReason.Value
                is not DeviceStateReason.NewActivation,

            _ => false,
        };

        if (secretsDone)
            ReleaseLock();
    }

    // NM rejects a path it no longer lists; "/" lets it find the access point by the profile
    private static string SpecificObject(WirelessNetwork network) =>
        network.IsRetained ? "/" : network.ObjectPath;

    private bool HasOtherAccessPoints(WirelessNetwork network) =>
        !string.IsNullOrEmpty(network.Bssid)
        && device.AvailableNetworks.Items.Any(other =>
            other != network && other.Ssid == network.Ssid
        );

    private void ReleaseLock()
    {
        if (lockedForActivation is null)
            return;

        var saved = lockedForActivation;
        lockedForActivation = null;

        try
        {
            saved.SetLockedBssid(null);
        }
        catch (GException ex)
        {
            // Not rethrown - it would hide the error of the activation, or break the state listeners
            System.Console.WriteLine($"Cannot unlock BSSID of {saved.ObjectPath}: {ex.Message}");
        }
    }

    /// <summary>Creates a profile for <paramref name="network"/> - NM completes the rest (ssid, mode, id) from the access point.</summary>
    private void AddAndActivate(WirelessNetwork network, string? password)
    {
        NetworkManagerBus.Call(
            "AddAndActivateConnection",
            NewProfileSettings(network.Security.KeyMgmt(), password),
            Variant.NewObjectPath(device.ObjectPath),
            Variant.NewObjectPath(network.ObjectPath)
        );
    }

    private static Variant NewProfileSettings(string? keyMgmt, string? password)
    {
        if (keyMgmt is null)
            return Variants.Dictionary("a{sv}");

        var security = password is null
            ? Variants.Dictionary("v", ("key-mgmt", Variant.NewString(keyMgmt)))
            : Variants.Dictionary(
                "v",
                ("key-mgmt", Variant.NewString(keyMgmt)),
                ("psk", Variant.NewString(password))
            );

        return Variants.Dictionary("a{sv}", (ConnectionSettings.WirelessSecurity, security));
    }
}
