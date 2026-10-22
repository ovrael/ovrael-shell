using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;
using OvraelShell.Services;

namespace OvraelShell.Widgets.Network;

[GObject.Subclass<Gtk.Box>]
public sealed partial class NetworkBarButton : IWithDisposableService<NetworkService>
{
    public NetworkService Service { get; private set; }
    private Button button;
    private NetworkPopover popover;

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
    public void AddService(NetworkService service)
    {
        Service = service;

        service.IsConnected.Changed += OnConnectionChanged;
        service.IsWireless.Changed += OnConnectionChanged;
        UpdateLabel();
    }

    public void RemoveService()
    {
        Service.IsConnected.Changed -= OnConnectionChanged;
        Service.IsWireless.Changed -= OnConnectionChanged;

        popover.RemoveService();
    }

    private void OnConnectionChanged<T>(T _) => UpdateLabel();

    [MemberNotNull(nameof(popover))]
    private void InitPopover(NetworkService network)
    {
        popover = NetworkPopover.New(network, button);
        button.OnClicked += (_, _) => popover.Popup();
    }

    private void UpdateLabel()
    {
        var icon = NetworkIcons.Connection(Service.IsConnected, Service.IsWireless);

        button.SetLabel(icon);
    }
}
