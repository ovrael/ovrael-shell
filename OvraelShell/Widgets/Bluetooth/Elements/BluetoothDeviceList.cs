using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.Models.Bluetooth;
using OvraelShell.Services;

namespace OvraelShell.Widgets.Bluetooth.Elements;

/// <summary>
/// Known (paired) or new (found) devices of the adapter as a list. A plain ListView rather
/// than a wrapping widget, so a ScrolledWindow can scroll it directly and rows stay recycled.
/// </summary>
public sealed class BluetoothDeviceList : IDisposable, IWithDisposableService<BluetoothService>
{
    public BluetoothService Service { get; private set; }
    private readonly Gio.ListStore model = Gio.ListStore.New(BluetoothDevice.GetGType());

    // Shows known or new devices of the adapter. Hides found devices without a name.
    private readonly CustomFilter filter;

    // Connected first, then by name - see CompareDevices
    private readonly CustomSorter sorter;

    private readonly NoSelection selection;

    /// <summary>The list shows known (paired) devices, else new (found) ones.</summary>
    public bool ShowsKnown { get; }

    public ListView View { get; }

    public bool IsEmpty => selection.GetNItems() == 0;

    /// <summary>Raised when devices are added to or removed from the list.</summary>
    public event Action? ItemsChanged;

    public BluetoothDeviceList(BluetoothService bluetoothService, bool showsKnown)
    {
        ShowsKnown = showsKnown;

        sorter = CustomSorter.New(CompareDevices);
        var sortedModel = SortListModel.New(model, sorter);

        filter = CustomFilter.New(IsShown);
        var filteredModel = FilterListModel.New(sortedModel, filter);

        // Rows have their own buttons - nothing to select
        selection = NoSelection.New(filteredModel);
        View = ListView.New(selection, CreateRowFactory());

        selection.OnItemsChanged += (_, _) => ItemsChanged?.Invoke();

        AddService(bluetoothService);
    }

    [MemberNotNull(nameof(Service))]
    public void AddService(BluetoothService service)
    {
        Service = service;

        foreach (var device in service.Devices.Items)
            AddDevice(device);

        service.Devices.Added += AddDevice;
        service.Devices.Removed += RemoveDevice;
        service.Adapter.Changed += OnAdapterChanged;
    }

    public void RemoveService() => Dispose();

    /// <summary>Stops following the service and its devices - they outlive the list.</summary>
    public void Dispose()
    {
        Service.Devices.Added -= AddDevice;
        Service.Devices.Removed -= RemoveDevice;
        Service.Adapter.Changed -= OnAdapterChanged;

        for (uint i = 0; i < model.GetNItems(); i++)
        {
            if (model.GetObject(i) is BluetoothDevice device)
                Unsubscribe(device);
        }

        model.RemoveAll();
    }

    #region Devices model

    private void AddDevice(BluetoothDevice device)
    {
        device.Paired.Changed += OnFilterKeyChanged;
        device.HasName.Changed += OnFilterKeyChanged;
        device.Connected.Changed += OnSortKeyChanged;
        device.Name.Changed += OnSortKeyChanged;

        model.Append(device);
    }

    private void RemoveDevice(BluetoothDevice device)
    {
        for (uint i = 0; i < model.GetNItems(); i++)
        {
            if ((BluetoothDevice?)model.GetObject(i) == device)
            {
                Unsubscribe(device);
                model.Remove(i);
                return;
            }
        }
    }

    private void Unsubscribe(BluetoothDevice device)
    {
        device.Paired.Changed -= OnFilterKeyChanged;
        device.HasName.Changed -= OnFilterKeyChanged;
        device.Connected.Changed -= OnSortKeyChanged;
        device.Name.Changed -= OnSortKeyChanged;
    }

    #endregion

    #region Filtering and sorting

    private void OnAdapterChanged(BluetoothAdapter? _) => filter.Changed(FilterChange.Different);

    // Paired also moves the device to the other list
    private void OnFilterKeyChanged(bool _) => filter.Changed(FilterChange.Different);

    private void OnSortKeyChanged<T>(T _) => sorter.Changed(SorterChange.Different);

    private bool IsShown(GObject.Object item) =>
        item is BluetoothDevice device
        && device.AdapterPath == Service.Adapter.Value?.ObjectPath
        && IsKnown(device) == ShowsKnown
        && (IsKnown(device) || device.HasName.Value);

    // BlueZ keeps a device paired after the first connection - that makes it known
    private static bool IsKnown(BluetoothDevice device) => device.Paired.Value;

    private static int CompareDevices(nint a, nint b)
    {
        var first = DeviceOf(a);
        var second = DeviceOf(b);

        var byConnected = second.Connected.Value.CompareTo(first.Connected.Value);
        if (byConnected != 0)
            return byConnected;

        return string.Compare(
            first.Name.Value,
            second.Name.Value,
            StringComparison.CurrentCultureIgnoreCase
        );
    }

    // The sorter gets raw GObject pointers - the model only holds devices
    private static BluetoothDevice DeviceOf(nint handle) =>
        (BluetoothDevice)
            GObject.Internal.InstanceWrapper.WrapHandle<BluetoothDevice>(handle, false);

    #endregion

    private static SignalListItemFactory CreateRowFactory()
    {
        var factory = SignalListItemFactory.New();

        factory.OnSetup += (_, args) =>
            ((ListItem)args.Object).SetChild(BluetoothDeviceListItem.New());

        factory.OnBind += (_, args) =>
        {
            var listItem = (ListItem)args.Object;

            if (
                listItem.GetChild() is BluetoothDeviceListItem row
                && listItem.GetItem() is BluetoothDevice device
            )
                row.Bind(device);
            else
                Console.WriteLine($"Cannot bind bluetooth list row {listItem}");
        };

        factory.OnUnbind += (_, args) =>
            (((ListItem)args.Object).GetChild() as BluetoothDeviceListItem)?.Unbind();

        return factory;
    }
}
