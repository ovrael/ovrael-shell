using Gio;
using OvraelShell.Enums.Network;
using OvraelShell.Interfaces;
using OvraelShell.Utils;

namespace OvraelShell.Models.Network;

[GObject.Subclass<GObject.Object>]
public sealed partial class WirelessNetwork
    : IWithObjectPath,
        IReactivable<WirelessNetwork>,
        IEquatable<WirelessNetwork>
{
    private DBusProxy? proxy;

    public string Ssid { get; set; } = string.Empty;
    public string Bssid { get; set; } = string.Empty;
    public ReactiveProperty<byte> Strength { get; set; } = new();

    /// <summary>Strength in the steps shown by the icon (0-3) - changes far less often than <see cref="Strength"/>.</summary>
    public ReactiveProperty<int> StrengthLevel { get; set; } = new();
    public ReactiveProperty<uint> Frequency { get; set; } = new();
    public ReactiveProperty<uint> Flags { get; set; } = new();
    public ReactiveProperty<uint> WpaFlags { get; set; } = new();
    public ReactiveProperty<uint> RsnFlags { get; set; } = new();

    /// <summary>Identity of the network - one access point in NetworkManager.</summary>
    public string ObjectPath { get; private set; } = string.Empty;
    public ReactiveProperty<bool> IsSaved { get; set; } = new();

    /// <summary>
    /// NM no longer lists the access point (ObjectPath is gone), but it is kept on the list
    /// until the next scan - see WirelessDevice. Connected to without a specific object.
    /// </summary>
    public bool IsRetained { get; set; }

    /// <summary>"Connecting..." or the last error; null when there is nothing to show.</summary>
    public ReactiveProperty<string?> ConnectStatus { get; set; } = new();

    /// <summary>The last attempt failed on authentication - ask for the password again.</summary>
    public ReactiveProperty<bool> PasswordRejected { get; set; } = new();
    public event Action<WirelessNetwork>? OnChange;

    public WirelessSecurity Security =>
        WirelessSecurityFlags.Decode(Flags.Value, WpaFlags.Value, RsnFlags.Value);

    /// <inheritdoc cref="WirelessSecurityFlags.HasPrivacy"/>
    public bool HasPrivacy => WirelessSecurityFlags.HasPrivacy(Flags.Value);

    partial void Initialize()
    {
        Strength.Changed += (_) => OnChange?.Invoke(this);
        Frequency.Changed += (_) => OnChange?.Invoke(this);
        Flags.Changed += (_) => OnChange?.Invoke(this);
        WpaFlags.Changed += (_) => OnChange?.Invoke(this);
        RsnFlags.Changed += (_) => OnChange?.Invoke(this);
        IsSaved.Changed += (_) => OnChange?.Invoke(this);
        ConnectStatus.Changed += (_) => OnChange?.Invoke(this);
        PasswordRejected.Changed += (_) => OnChange?.Invoke(this);
    }

    /// <summary>Creates a network for an access point, or null for a hidden network (no SSID).</summary>
    public static WirelessNetwork? Create(string objectPath)
    {
        var proxy = NetworkManagerBus.CreateProxy(
            objectPath,
            NetworkManagerBus.AccessPointInterface
        );

        var ssid = ProxyGets.ByteString(proxy, "Ssid");

        if (string.IsNullOrEmpty(ssid))
        {
            proxy.Dispose();
            return null;
        }

        var network = NewWithProperties([]);
        network.proxy = proxy;
        network.Ssid = ssid;
        network.Bssid = ProxyGets.String(proxy, "HwAddress") ?? string.Empty;
        network.ObjectPath = objectPath;

        ProxyGets.Bind(
            proxy,
            network.UpdateProperty,
            "Strength",
            "Frequency",
            "Flags",
            "WpaFlags",
            "RsnFlags"
        );

        return network;
    }

    private void UpdateProperty(string property)
    {
        if (proxy is null)
            return;

        switch (property)
        {
            case "Strength":
                Strength.Set(ProxyGets.Byte(proxy, property));
                StrengthLevel.Set(StrengthLevelOf(Strength.Value));
                break;

            case "Frequency":
                Frequency.Set(ProxyGets.UInt(proxy, property));
                break;

            case "Flags":
                Flags.Set(ProxyGets.UInt(proxy, property));
                break;

            case "WpaFlags":
                WpaFlags.Set(ProxyGets.UInt(proxy, property));
                break;

            case "RsnFlags":
                RsnFlags.Set(ProxyGets.UInt(proxy, property));
                break;
        }
    }

    public static int StrengthLevelOf(uint strength) =>
        strength switch
        {
            < 15 => 0,
            < 45 => 1,
            < 75 => 2,
            _ => 3,
        };

    /// <summary>
    /// Releases the D-Bus proxy. Not named Dispose, because that would free the GObject,
    /// which may still be held by a widget's ListStore.
    /// </summary>
    public void Release()
    {
        proxy?.Dispose();
        proxy = null;
    }

    #region Equality

    // Wrappers from GTK models (ListStore, ListItem) are compared by path, not by reference

    public bool Equals(WirelessNetwork? other) =>
        other is not null && ObjectPath == other.ObjectPath;

    public override bool Equals(object? obj) => Equals(obj as WirelessNetwork);

    public override int GetHashCode() => ObjectPath.GetHashCode();

    public static bool operator ==(WirelessNetwork? left, WirelessNetwork? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(WirelessNetwork? left, WirelessNetwork? right) =>
        !(left == right);

    #endregion

    public override string ToString() =>
        $"WifiNetwork {{ Ssid = {Ssid}, Bssid = {Bssid}, Strength = {Strength}%, "
        + $"Frequency = {Frequency} MHz, Flags = {Flags}, WpaFlags = {WpaFlags}, "
        + $"RsnFlags = {RsnFlags}, IsSaved = {IsSaved}, ObjectPath = {ObjectPath} }}";
}
