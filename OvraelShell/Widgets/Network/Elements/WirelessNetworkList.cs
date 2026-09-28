using Gtk;
using OvraelShell.Enums.Network;
using OvraelShell.Models.Network;
using OvraelShell.Services;

namespace OvraelShell.Widgets.Elements.Network;

/// <summary>
/// Access points of the Wi-Fi device as a list with Connect buttons. A plain ListView rather than
/// a wrapping widget, so a ScrolledWindow can scroll it directly and rows stay recycled.
/// </summary>
public sealed class WirelessNetworkList : IDisposable
{
    private readonly NetworkService networkService;
    private readonly Gio.ListStore model = Gio.ListStore.New(WirelessNetwork.GetGType());

    // Hides the active network - it is shown in CurrentConnectionBox instead
    private readonly CustomFilter activeNetworkFilter;

    // Saved first, then by signal - see CompareNetworks
    private readonly CustomSorter networkSorter;

    private WirelessDevice? device;

    public ListView View { get; }

    /// <summary>
    /// Raised before the view lays out a change of its rows (added, removed, status shown) -
    /// the view still has its old size, see NetworkPopover.HoldListHeight.
    /// </summary>
    public event Action? RowsChanging;

    public WirelessNetworkList(NetworkService networkService)
    {
        this.networkService = networkService;

        networkSorter = CustomSorter.New(CompareNetworks);
        var sortedModel = SortListModel.New(model, networkSorter);

        activeNetworkFilter = CustomFilter.New(IsNotActiveNetwork);
        var filteredModel = FilterListModel.New(sortedModel, activeNetworkFilter);

        var selection = SingleSelection.New(filteredModel);
        View = ListView.New(selection, CreateRowFactory());

        // Connected after the view's own handler, so it runs once the view has taken the change
        selection.OnItemsChanged += (_, _) =>
        {
            KeepScrolledToTop(selection.GetNItems());
            RowsChanging?.Invoke();
        };

        networkService.WirelessDevice.Changed += SetDevice;
        SetDevice(networkService.WirelessDevice.Value);
    }

    /// <summary>Stops following the service, its device and networks - they outlive the list.</summary>
    public void Dispose()
    {
        networkService.WirelessDevice.Changed -= SetDevice;
        SetDevice(null);
    }

    #region Wireless device

    private void SetDevice(WirelessDevice? newDevice)
    {
        if (device is not null)
        {
            device.AvailableNetworks.Added -= AddNetwork;
            device.AvailableNetworks.Removed -= RemoveNetwork;
            device.AvailableNetworks.Cleared -= ClearNetworks;
            device.ActiveNetwork.Changed -= OnActiveNetworkChanged;
            device.State.Changed -= OnDeviceStateChanged;
        }

        device = newDevice;

        ClearNetworks();
        RefilterActive();

        if (device is null)
            return;

        foreach (var network in device.AvailableNetworks.Items)
            AddNetwork(network);

        device.AvailableNetworks.Added += AddNetwork;
        device.AvailableNetworks.Removed += RemoveNetwork;
        device.AvailableNetworks.Cleared += ClearNetworks;
        device.ActiveNetwork.Changed += OnActiveNetworkChanged;
        device.State.Changed += OnDeviceStateChanged;
    }

    private void OnActiveNetworkChanged(WirelessNetwork? _) => RefilterActive();

    private void OnDeviceStateChanged(DeviceState _) => RefilterActive();

    private void RefilterActive() => activeNetworkFilter.Changed(FilterChange.Different);

    /// <summary>
    /// Hides the active network once it is connected. While connecting (or when the password
    /// is rejected) it stays in the list, so the status and password field are visible.
    /// </summary>
    private bool IsNotActiveNetwork(GObject.Object item)
    {
        var active = device?.ActiveNetwork.Value;

        return active is null
            || device?.State.Value != DeviceState.Activated
            || (item as WirelessNetwork) != active;
    }

    #endregion

    #region Networks model

    private void AddNetwork(WirelessNetwork network)
    {
        network.IsSaved.Changed += OnSortKeyChanged;
        network.StrengthLevel.Changed += OnSortKeyChanged;

        // Status line and password field change the height of the row
        network.ConnectStatus.Changed += OnRowContentChanged;
        network.PasswordRejected.Changed += OnRowContentChanged;

        model.Append(network);
    }

    private void ClearNetworks()
    {
        for (uint i = 0; i < model.GetNItems(); i++)
        {
            if (model.GetObject(i) is WirelessNetwork network)
                Unsubscribe(network);
        }

        model.RemoveAll();
    }

    private void RemoveNetwork(WirelessNetwork network)
    {
        for (uint i = 0; i < model.GetNItems(); i++)
        {
            if ((WirelessNetwork?)model.GetObject(i) == network)
            {
                Unsubscribe(network);
                model.Remove(i);
                return;
            }
        }
    }

    private void Unsubscribe(WirelessNetwork network)
    {
        network.IsSaved.Changed -= OnSortKeyChanged;
        network.StrengthLevel.Changed -= OnSortKeyChanged;
        network.ConnectStatus.Changed -= OnRowContentChanged;
        network.PasswordRejected.Changed -= OnRowContentChanged;
    }

    private void OnRowContentChanged<T>(T _) => RowsChanging?.Invoke();

    #endregion

    /// <summary>
    /// The view keeps the row that was at the top in place, so a network sorted in above it
    /// (e.g. the one just disconnected from) ends up hidden above the view. When the list
    /// was scrolled to the top, keep it there - the adjustment still has the old value,
    /// the view updates it on the next allocation.
    /// </summary>
    private void KeepScrolledToTop(uint count)
    {
        var adjustment = View.GetVadjustment();

        if (adjustment is null || adjustment.GetValue() > adjustment.GetLower())
            return;

        if (count > 0)
            View.ScrollTo(0, ListScrollFlags.None, null);
    }

    #region Sorting

    private void OnSortKeyChanged<T>(T _) => networkSorter.Changed(SorterChange.Different);

    /// <summary>
    /// Saved networks first, then stronger signal (by icon level, so rows do not jump on
    /// every small change), then by name. Bands of the same network: the higher one first.
    /// </summary>
    private static int CompareNetworks(nint a, nint b)
    {
        var first = NetworkOf(a);
        var second = NetworkOf(b);

        var bySaved = second.IsSaved.Value.CompareTo(first.IsSaved.Value);
        if (bySaved != 0)
            return bySaved;

        var byStrength = second.StrengthLevel.Value.CompareTo(first.StrengthLevel.Value);
        if (byStrength != 0)
            return byStrength;

        var byName = string.Compare(
            first.Ssid,
            second.Ssid,
            StringComparison.CurrentCultureIgnoreCase
        );
        if (byName != 0)
            return byName;

        return second.Frequency.Value.CompareTo(first.Frequency.Value);
    }

    // The sorter gets raw GObject pointers - the model only holds networks
    private static WirelessNetwork NetworkOf(nint handle) =>
        (WirelessNetwork)GObject.Internal.InstanceWrapper.WrapHandle<WirelessNetwork>(handle, false);

    #endregion

    private SignalListItemFactory CreateRowFactory()
    {
        var factory = SignalListItemFactory.New();

        factory.OnSetup += (_, args) =>
        {
            var row = NetworkWirelessListItem.New();

            // Once per row widget - rows are reused, so this does not pile up
            row.ConnectRequested += Connect;

            ((ListItem)args.Object).SetChild(row);
        };

        factory.OnBind += (_, args) =>
        {
            var listItem = (ListItem)args.Object;

            if (
                listItem.GetChild() is NetworkWirelessListItem row
                && listItem.GetItem() is WirelessNetwork network
            )
                row.Bind(network);
            else
                System.Console.WriteLine($"Cannot bind network list row {listItem}");
        };

        factory.OnUnbind += (_, args) =>
            (((ListItem)args.Object).GetChild() as NetworkWirelessListItem)?.Unbind();

        return factory;
    }

    private void Connect(NetworkWirelessListItem row, WirelessNetwork network, string? password)
    {
        try
        {
            // Progress and errors are shown from the network model (ConnectStatus)
            if (networkService.ConnectWireless(network, password) == ConnectResult.PasswordRequired)
                row.AskForPassword();
        }
        catch (Exception ex)
            when (ex is GLib.GException or NotSupportedException or InvalidOperationException)
        {
            System.Console.WriteLine($"Cannot connect to {network.Ssid}: {ex.Message}");

            // No Wi-Fi device - the model has nowhere to report it
            if (ex is InvalidOperationException)
                row.ShowStatus(ex.Message);
        }
    }
}
