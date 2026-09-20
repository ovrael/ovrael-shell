using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;
using OvraelShell.Services;
using OvraelShell.ThemeManager;

namespace OvraelShell.Widgets;

[GObject.Subclass<Gtk.Box>]
public sealed partial class InternetWidget
{
    private Button button;
    private Popover popover;
    private NetworkService network;

    public static InternetWidget New(NetworkService network)
    {
        var widget = NewWithProperties([]);
        widget.AddNetworkService(network);
        return widget;
    }

    [MemberNotNull(nameof(network))]
    private void AddNetworkService(NetworkService network)
    {
        this.network = network;
        network.IsWifi.Changed += (v) =>
            UpdateLabel(network.IsConnected, v, network.SignalStrength);
        network.IsConnected.Changed += (v) =>
            UpdateLabel(v, network.IsWifi, network.SignalStrength);
        network.SignalStrength.Changed += (v) =>
            UpdateLabel(network.IsConnected, network.IsWifi, v);

        UpdateLabel(network.IsConnected, network.IsWifi, network.SignalStrength);

        System.Console.WriteLine("Available networks");
        foreach (var wifiNetwork in network.AvailableNetworks.Value)
        {
            System.Console.WriteLine(wifiNetwork);
        }
    }

    private void UpdateLabel(bool isConnected, bool isWifi, byte strength)
    {
        string icon = isConnected
            ? isWifi
                ? Icons.Network.WifiConnected
                : Icons.Network.EthernetConnected
            : isWifi
                ? Icons.Network.WifiDisconnected
                : Icons.Network.EthernetDisconnected;

        string newLabel = isWifi ? $"{icon} {strength}" : icon;

        button.Label = newLabel;
    }

    [MemberNotNull(nameof(button), nameof(popover))]
    partial void Initialize()
    {
        SetOrientation(Orientation.Horizontal);

        var popoverContent = CreatePopoverContent();
        button = CreateButton();
        popover = CreatePopover(popoverContent);
        button.OnClicked += (_, _) =>
        {
            popover.Popup();
        };

        Append(button);
    }

    private Button CreateButton()
    {
        var button = Button.NewWithLabel(Icons.Network.EthernetDisconnected);
        button.SetCursor(Cursors.Pointer);

        return button;
    }

    private Popover CreatePopover(Box content)
    {
        var popover = Popover.New();
        popover.SetParent(button);
        popover.SetPosition(PositionType.Top);
        popover.SetAutohide(true);
        popover.SetChild(content);
        popover.AddCssClass("popover");

        return popover;
    }

    private Box CreatePopoverContent()
    {
        var content = New(Orientation.Vertical, 8);
        content.SetMarginTop(12);
        content.SetMarginBottom(12);
        content.SetMarginStart(12);
        content.SetMarginEnd(12);

        content.Append(Label.New("Available networks"));
        content.Append(Button.NewWithLabel("Wi-Fi 1"));
        content.Append(Button.NewWithLabel("Wi-Fi 2"));
        content.Append(Button.NewWithLabel("Ethernet"));
        return content;
    }

    // private ListView CreateNetworksListView()
    // {

    // }
}
