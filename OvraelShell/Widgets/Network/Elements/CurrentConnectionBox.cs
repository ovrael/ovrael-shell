using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.Enums.Network;
using OvraelShell.InterfaceElements;
using OvraelShell.Models.Network;
using OvraelShell.Services;

namespace OvraelShell.Widgets.Network.Elements;

[GObject.Subclass<Gtk.Box>]
public sealed partial class CurrentConnectionBox : IWithDisposableService<NetworkService>
{
    private Label iconLabel;
    private Label nameLabel;
    private Label detailsLabel;
    private Label stateLabel;
    private Label statsLabel;
    private TrafficContainer downloadTraffic;
    private TrafficContainer uploadTraffic;

    // Of the primary device, measured only while the box is on screen
    private readonly Models.Network.NetworkTraffic traffic = new();

    // Network whose OnChange is attached - networks outlive rows in the popover, so detach on change
    private WirelessNetwork? trackedNetwork;

    public NetworkService Service { get; private set; }

    public static CurrentConnectionBox New()
    {
        return NewWithProperties([]);
    }

    partial void Initialize()
    {
        SetOrientation(Orientation.Vertical);
        SetMarginTop(12);
        SetMarginBottom(12);
        SetMarginStart(12);
        SetMarginEnd(12);
        SetSpacing(4);

        AppendContent();
        AddCssClass("current-connection-box");

        traffic.Rate.Changed += (_) => UpdateTraffic();
        OnMap += (_, _) => traffic.Start();
        OnUnmap += (_, _) => traffic.Stop();
        OnDestroy += (_, _) => traffic.Dispose();
    }

    [MemberNotNull(
        nameof(iconLabel),
        nameof(nameLabel),
        nameof(stateLabel),
        nameof(statsLabel),
        nameof(uploadTraffic),
        nameof(downloadTraffic)
    )]
    private void AppendContent()
    {
        SetOrientation(Orientation.Horizontal);
        SetSpacing(10);

        iconLabel = Label.New(Icons.Network.EthernetDisconnected);

        // Text of the header changes while connecting - it must not change the popover width.
        // Ellipsized with a tiny natural width, the name takes whatever space is left.
        // BSSID and interface follow the name in parentheses - they are cut first.
        nameLabel = Label.New("Not connected");
        nameLabel.SetXalign(0);
        nameLabel.SetEllipsize(Pango.EllipsizeMode.End);
        nameLabel.SetMaxWidthChars(30);
        nameLabel.Hexpand = true;

        detailsLabel = Label.New(string.Empty);
        detailsLabel.SetXalign(0);

        stateLabel = Label.New(string.Empty); // Connected etc.
        stateLabel.SetXalign(1);

        statsLabel = Label.New(string.Empty);
        statsLabel.Hexpand = true;
        statsLabel.SetXalign(0);
        statsLabel.AddCssClass("connection-stats-label");

        downloadTraffic = TrafficContainer.New(Icons.Network.Download);
        uploadTraffic = TrafficContainer.New(Icons.Network.Upload);

        var leftColumn = Box.New(Orientation.Vertical, 10);
        leftColumn.SetHalign(Align.Start);
        var rightColumn = Box.New(Orientation.Vertical, 10);
        rightColumn.SetHalign(Align.End);

        var basicDataRow = Box.New(Orientation.Horizontal, 10);
        basicDataRow.Append(iconLabel);
        basicDataRow.Append(nameLabel);

        leftColumn.Append(basicDataRow);
        leftColumn.Append(statsLabel);
        leftColumn.Append(detailsLabel);

        rightColumn.Append(stateLabel);
        rightColumn.Append(downloadTraffic);
        rightColumn.Append(uploadTraffic);

        Append(leftColumn);
        Append(rightColumn);
    }

    private Label CreateTrafficLabelContainer()
    {
        var trafficLabel = Label.New(string.Empty);
        trafficLabel.SetXalign(0);
        trafficLabel.SetWidthChars(10);
        trafficLabel.AddCssClass("traffic-label");
        return trafficLabel;
    }

    private Label CreateTrafficLabel()
    {
        var trafficLabel = Label.New(string.Empty);
        trafficLabel.SetXalign(0);
        trafficLabel.SetWidthChars(10);
        trafficLabel.AddCssClass("traffic-label");
        return trafficLabel;
    }

    [MemberNotNull(nameof(Service))]
    public void AddService(NetworkService networkService)
    {
        Service = networkService;

        networkService.IsConnected.Changed += OnValueChanged;
        networkService.PrimaryConnectionType.Changed += OnValueChanged;

        foreach (var device in networkService.Devices.Items)
            WatchDevice(device);

        networkService.Devices.Added += OnDeviceAdded;
        networkService.Devices.Removed += OnDeviceRemoved;

        Refresh();
    }

    public void RemoveService()
    {
        Service.IsConnected.Changed -= OnValueChanged;
        Service.PrimaryConnectionType.Changed -= OnValueChanged;
        Service.Devices.Added -= OnDeviceAdded;
        Service.Devices.Removed -= OnDeviceRemoved;

        foreach (var device in Service.Devices.Items)
            UnwatchDevice(device);

        TrackNetwork(null);
        traffic.Stop();
    }

    private void OnValueChanged<T>(T _) => Refresh();

    private void OnDeviceAdded(NetworkDevice device)
    {
        WatchDevice(device);
        Refresh();
    }

    // Before the device is disposed - disposing clears its active network
    private void OnDeviceRemoved(NetworkDevice device)
    {
        UnwatchDevice(device);
        Refresh();
    }

    private void WatchDevice(NetworkDevice device)
    {
        // Primary device is the one with an active connection - it can switch between devices
        device.State.Changed += OnValueChanged;
        device.ActiveConnectionPath.Changed += OnValueChanged;

        switch (device)
        {
            case WirelessDevice wireless:
                wireless.ActiveNetwork.Changed += OnValueChanged;
                break;

            case EthernetDevice ethernet:
                ethernet.Speed.Changed += OnValueChanged;
                ethernet.Carrier.Changed += OnValueChanged;
                break;
        }
    }

    private void UnwatchDevice(NetworkDevice device)
    {
        device.State.Changed -= OnValueChanged;
        device.ActiveConnectionPath.Changed -= OnValueChanged;

        switch (device)
        {
            case WirelessDevice wireless:
                wireless.ActiveNetwork.Changed -= OnValueChanged;
                break;

            case EthernetDevice ethernet:
                ethernet.Speed.Changed -= OnValueChanged;
                ethernet.Carrier.Changed -= OnValueChanged;
                break;
        }
    }

    private void TrackNetwork(WirelessNetwork? network)
    {
        if (network == trackedNetwork)
            return;

        trackedNetwork?.OnChange -= OnNetworkChanged;
        trackedNetwork = network;
        trackedNetwork?.OnChange += OnNetworkChanged;
    }

    private void OnNetworkChanged(WirelessNetwork _) => Refresh();

    private void Refresh()
    {
        var device = Service.PrimaryDevice;
        var network = (device as WirelessDevice)?.ActiveNetwork.Value;

        TrackNetwork(network);

        // PrimaryDevice is the one with an active connection
        traffic.SetInterface(device?.InterfaceName);
        UpdateTraffic();

        switch (device)
        {
            case WirelessDevice wireless:
                ShowWireless(wireless, network);
                break;

            case EthernetDevice ethernet:
                ShowEthernet(ethernet);
                break;

            default:
                ShowDisconnected();
                break;
        }
    }

    private void ShowWireless(WirelessDevice device, WirelessNetwork? network)
    {
        iconLabel.SetLabel(Icons.Network.WifiConnected);
        stateLabel.SetLabel(device.State.Value.ToDisplayText());

        if (network is null)
        {
            SetBaseData("Wi-Fi", device.InterfaceName);
            SetStats(string.Empty, null);
            return;
        }

        SetBaseData(network.Ssid, $"{network.Bssid} · {device.InterfaceName}");

        var strength = network.Strength.Value;
        var frequency = network.Frequency.Value;

        SetStats(
            $"{NetworkIcons.Strength(strength)}  {strength}%   "
                + $"{NetworkIcons.Frequency(frequency)}  {frequency / 1000.0:0.0} GHz   "
                + $"{NetworkIcons.Privacy(network.HasPrivacy)}  {SecurityText(network.Security)}",
            $"Signal strength: {strength}%\nFrequency: {frequency} MHz\nSecurity: {SecurityText(network.Security)}"
        );
    }

    private void SetBaseData(string name, string? details)
    {
        nameLabel.SetLabel(name);
        detailsLabel.SetLabel(details ?? string.Empty);
    }

    private void SetStats(string text, string? tooltip)
    {
        statsLabel.SetLabel(text);
        statsLabel.TooltipText = tooltip;
    }

    private static string SecurityText(WirelessSecurity security)
    {
        return security switch
        {
            WirelessSecurity.None => "Open",
            WirelessSecurity.Owe => "Enhanced Open",
            WirelessSecurity.WpaPsk => "WPA2",
            WirelessSecurity.Sae => "WPA3",
            WirelessSecurity.Enterprise => "Enterprise",
            WirelessSecurity.Wep => "WEP",
            _ => security.ToString(),
        };
    }

    private void UpdateTraffic()
    {
        if (traffic.InterfaceName is null)
        {
            downloadTraffic.SetEmpty();
            return;
        }

        var rate = traffic.Rate.Value;

        var (dSpeed, dUnit) = FormatSpeed(rate.Download);
        downloadTraffic.UpdateLabels(dSpeed, dUnit);

        var (uSpeed, uUnit) = FormatSpeed(rate.Upload);
        uploadTraffic.UpdateLabels(uSpeed, uUnit);
    }

    private static (string Speed, string Unit) FormatSpeed(double bytesPerSecond)
    {
        return bytesPerSecond switch
        {
            < 1024 => ($"{bytesPerSecond:0}", "B/s"),
            < 1024 * 1024 => ($"{bytesPerSecond / 1024:0.0}", "KB/s"),
            < 1024 * 1024 * 1024 => ($"{bytesPerSecond / (1024 * 1024):0.0}", "MB/s"),
            _ => ($"{bytesPerSecond / (1024 * 1024 * 1024):0.0}", "GB/s"),
        };
    }

    private void ShowEthernet(EthernetDevice device)
    {
        iconLabel.SetLabel(NetworkIcons.Ethernet(device.Carrier));
        SetBaseData("Ethernet", device.InterfaceName);
        stateLabel.SetLabel(device.State.Value.ToDisplayText());

        var speed = device.Speed.Value;

        if (speed > 0)
            SetStats($"{Icons.Common.SpeedFast}  {speed} Mb/s", $"Link speed: {speed} Mb/s");
        else
            SetStats(string.Empty, null);
    }

    private void ShowDisconnected()
    {
        iconLabel.SetLabel(Icons.Network.WifiDisconnected);
        SetBaseData("Not connected", null);
        stateLabel.SetLabel(string.Empty);
        SetStats(string.Empty, null);
    }
}
