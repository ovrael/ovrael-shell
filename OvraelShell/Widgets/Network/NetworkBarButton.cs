using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;
using OvraelShell.Models.Network;
using OvraelShell.Services;
using OvraelShell.Utils;

namespace OvraelShell.Widgets.Network;

[GObject.Subclass<Gtk.Box>]
public sealed partial class NetworkBarButton : IWithDisposableService<NetworkService>
{
    public NetworkService Service { get; private set; }
    private Button button;
    private NetworkPopover popover;
    private WirelessNetwork? activeNetwork;

    public static NetworkBarButton New(NetworkService network)
    {
        var widget = NewWithProperties([]);
        widget.AddService(network);
        widget.InitPopover(network);
        return widget;
    }

    [MemberNotNull(nameof(button))]
    partial void Initialize()
    {
        SetOrientation(Orientation.Horizontal);

        button = Button.NewWithLabel(Icons.Network.EthernetDisconnected);
        button.SetCursor(Cursors.Pointer);
        Append(button);
    }

    [MemberNotNull(nameof(Service))]
    public void AddService(NetworkService network)
    {
        Service = network;

        network.IsConnected.Changed += OnConnectionChanged;
        network.PrimaryConnectionType.Changed += OnConnectionChanged;

        network.WirelessDevice.Changed += SetWirelessDevice;
        SetWirelessDevice(network.WirelessDevice.Value);
    }

    public void RemoveService()
    {
        Service.IsConnected.Changed -= OnConnectionChanged;
        Service.PrimaryConnectionType.Changed -= OnConnectionChanged;
        Service.WirelessDevice.Changed -= SetWirelessDevice;

        // The device and its networks belong to the service too
        wirelessDevice?.ActiveNetwork.Changed -= SetActiveNetwork;
        wirelessDevice = null;
        activeNetwork?.Strength.Changed -= OnStrengthChanged;
        activeNetwork = null;

        popover.RemoveService();
    }

    private void OnConnectionChanged<T>(T _) => UpdateLabel();

    [MemberNotNull(nameof(popover))]
    private void InitPopover(NetworkService network)
    {
        popover = NetworkPopover.New(network, button);
        button.OnClicked += (_, _) => popover.Popup();
    }

    #region Wireless device

    private WirelessDevice? wirelessDevice;

    private void SetWirelessDevice(WirelessDevice? device)
    {
        wirelessDevice?.ActiveNetwork.Changed -= SetActiveNetwork;
        wirelessDevice = device;
        wirelessDevice?.ActiveNetwork.Changed += SetActiveNetwork;

        SetActiveNetwork(wirelessDevice?.ActiveNetwork.Value);
    }

    private void SetActiveNetwork(WirelessNetwork? network)
    {
        activeNetwork?.Strength.Changed -= OnStrengthChanged;
        activeNetwork = network;
        activeNetwork?.Strength.Changed += OnStrengthChanged;

        UpdateLabel();
    }

    private void OnStrengthChanged(byte _) => UpdateLabel();

    #endregion

    private void UpdateLabel()
    {
        var isWifi =
            Service.PrimaryConnectionType.Value == NetworkManagerBus.WirelessConnectionType;
        var icon = NetworkIcons.Connection(Service.IsConnected, isWifi);

        button.SetLabel(icon);
    }
}
