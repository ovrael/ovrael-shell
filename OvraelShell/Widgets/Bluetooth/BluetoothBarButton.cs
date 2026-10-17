using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;
using OvraelShell.Models.Bluetooth;
using OvraelShell.Services;

namespace OvraelShell.Widgets.Bluetooth;

[GObject.Subclass<Gtk.Box>]
public sealed partial class BluetoothBarButton : IWithDisposableService<BluetoothService>
{
    public BluetoothService Service { get; private set; }
    private Button button;
    private BluetoothPopover popover;
    private BluetoothAdapter? adapter;

    public static BluetoothBarButton New(BluetoothService bluetooth)
    {
        var widget = NewWithProperties([]);
        widget.AddService(bluetooth);
        widget.InitPopover(bluetooth);
        return widget;
    }

    [MemberNotNull(nameof(button))]
    partial void Initialize()
    {
        SetOrientation(Orientation.Horizontal);

        button = Button.NewWithLabel(Icons.Bluetooth.Off);
        button.SetCursor(Cursors.Pointer);
        Append(button);
    }

    [MemberNotNull(nameof(Service))]
    public void AddService(BluetoothService bluetooth)
    {
        Service = bluetooth;

        bluetooth.ConnectedDevices.Changed += OnConnectedDevicesChanged;
        bluetooth.Adapter.Changed += SetAdapter;
        SetAdapter(bluetooth.Adapter.Value);
    }

    public void RemoveService()
    {
        Service.ConnectedDevices.Changed -= OnConnectedDevicesChanged;
        Service.Adapter.Changed -= SetAdapter;

        // The adapter belongs to the service too
        adapter?.Powered.Changed -= OnPoweredChanged;
        adapter = null;

        popover.RemoveService();
    }

    [MemberNotNull(nameof(popover))]
    private void InitPopover(BluetoothService bluetooth)
    {
        popover = BluetoothPopover.New(bluetooth, button);
        button.OnClicked += (_, _) => popover.Popup();
    }

    private void SetAdapter(BluetoothAdapter? newAdapter)
    {
        adapter?.Powered.Changed -= OnPoweredChanged;
        adapter = newAdapter;
        adapter?.Powered.Changed += OnPoweredChanged;

        UpdateLabel();
    }

    private void OnPoweredChanged(bool _) => UpdateLabel();

    private void OnConnectedDevicesChanged(IReadOnlyList<BluetoothDevice> _) => UpdateLabel();

    private void UpdateLabel()
    {
        var isPowered = adapter?.Powered.Value == true;
        var connected = Service.ConnectedDevices.Value;

        button.SetLabel(BluetoothIcons.State(isPowered, connected.Count > 0));
        button.SetTooltipText(
            !isPowered ? "Bluetooth is off"
            : connected.Count > 0 ? string.Join("\n", connected.Select(device => device.Name.Value))
            : "No device connected"
        );
    }
}
