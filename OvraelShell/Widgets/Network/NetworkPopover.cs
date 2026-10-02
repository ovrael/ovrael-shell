using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.Services;
using OvraelShell.Utils;
using OvraelShell.Widgets.Network.Elements;
using ZwlrLayerShell;

namespace OvraelShell.Widgets.Network;

[GObject.Subclass<Gtk.Popover>]
public sealed partial class NetworkPopover : IWithDisposableService<NetworkService>
{
    // Share of the monitor height the whole popover may take
    private const double MaxHeightRatio = 0.45;

    // Content margins (2 x 12) plus .popover contents padding and border (2 x 10 + 2 x 1)
    private const int PopoverChrome = 24 + 22;

    public NetworkService Service { get; private set; }

    // Created in Initialize, which runs before AddNetworkService
    private Box content;
    private CurrentConnectionBox currentConnectionBox;
    private Label networksTitle;
    private ScrolledWindow networksScroll;

    // Needs the service, so it is created in AddNetworkService
    private WirelessNetworkList? networkList;

    public static NetworkPopover New(NetworkService networkService, Button parent)
    {
        var widget = NewWithProperties([]);
        widget.AddService(networkService);
        widget.SetParent(parent);
        return widget;
    }

    [MemberNotNull(
        nameof(content),
        nameof(currentConnectionBox),
        nameof(networksTitle),
        nameof(networksScroll)
    )]
    partial void Initialize()
    {
        CreateContent();

        SetPosition(PositionType.Top);
        SetAutohide(true);
        SetChild(content);
        AddCssClass("popover");

        OnShow += (_, _) => OnShown();
        OnHide += (_, _) => OnHidden();
    }

    [MemberNotNull(nameof(Service))]
    public void AddService(NetworkService networkService)
    {
        Service = networkService;

        currentConnectionBox.AddService(networkService);

        networkList = new WirelessNetworkList(networkService);
        networkList.RowsChanging += HoldListHeight;
        networksScroll.SetChild(networkList.View);
    }

    public void RemoveService()
    {
        currentConnectionBox.RemoveService();

        if (networkList is null)
            return;

        // Before Dispose - it empties the list, which would resize the popover
        networkList.RowsChanging -= HoldListHeight;
        networkList.Dispose();
    }

    [MemberNotNull(
        nameof(content),
        nameof(currentConnectionBox),
        nameof(networksTitle),
        nameof(networksScroll)
    )]
    private void CreateContent()
    {
        content = Box.New(Orientation.Vertical, 8);
        content.SetMarginTop(12);
        content.SetMarginBottom(12);
        content.SetMarginStart(12);
        content.SetMarginEnd(12);

        currentConnectionBox = CurrentConnectionBox.New();
        content.Append(currentConnectionBox);

        networksTitle = Label.New("Available networks");
        content.Append(networksTitle);

        // Grows with the list up to the max height, then scrolls
        networksScroll = ScrolledWindow.New();
        networksScroll.SetPolicy(PolicyType.Never, PolicyType.Automatic);
        networksScroll.SetPropagateNaturalHeight(true);
        content.Append(networksScroll);
    }

    private void OnShown()
    {
        // Keyboard only while open - needed to type the Wi-Fi password
        SetBarKeyboardMode(KeyboardMode.OnDemand);

        // Fit the list anew on every show - before the max, which may not go below the min
        networksScroll.SetMinContentHeight(-1);
        UpdateMaxHeight();

        try
        {
            Service.RequestWifiScan();
        }
        catch (GLib.GException ex)
        {
            // NM throws exception when on scan is on cooldown
            System.Console.WriteLine(ex.Message);
        }
    }

    // Hand the keyboard back when closing. With OnDemand the compositor keeps
    // focus on the bar until another window is clicked, so typing elsewhere
    // would not work after the popover closes.
    private void OnHidden()
    {
        GetParentWindow()?.SetFocus(null);
        SetBarKeyboardMode(KeyboardMode.None);
    }

    /// <summary>
    /// Keeps the list from shrinking while the popover is open. Every size change makes GTK
    /// present the popup again (xdg_popup reposition), which flickers on Hyprland - and
    /// switching networks changes the rows many times: NM drops the access points and finds
    /// them again, the active network hides and shows, status lines come and go.
    /// Called before the change is laid out, so the current height is the one to keep.
    /// </summary>
    private void HoldListHeight()
    {
        if (!GetVisible())
            return;

        var height = networksScroll.GetHeight();
        var maxHeight = networksScroll.GetMaxContentHeight();

        if (maxHeight >= 0)
            height = Math.Min(height, maxHeight);

        if (height > networksScroll.GetMinContentHeight())
            networksScroll.SetMinContentHeight(height);
    }

    private Gtk.Window? GetParentWindow() => GetParent()?.GetRoot() as Gtk.Window;

    private void SetBarKeyboardMode(KeyboardMode mode)
    {
        if (GetParentWindow() is { } window && LayerShell.IsLayerWindow(window))
            LayerShell.SetKeyboardMode(window, mode);
    }

    /// <summary>
    /// Limits the list so the whole popover takes about <see cref="MaxHeightRatio"/> of the
    /// monitor the bar is on. Done on every show - the bar can move between monitors.
    /// </summary>
    private void UpdateMaxHeight()
    {
        var monitorHeight = GetParent() is { } parent ? MonitorSize.HeightOf(parent) : 0;

        if (monitorHeight <= 0)
            return;

        // Everything above the list: current connection, title and the gaps between them
        currentConnectionBox.Measure(
            Orientation.Vertical,
            -1,
            out _,
            out var headerHeight,
            out _,
            out _
        );
        networksTitle.Measure(Orientation.Vertical, -1, out _, out var titleHeight, out _, out _);

        var reserved = headerHeight + titleHeight + 2 * content.GetSpacing() + PopoverChrome;
        var maxListHeight = Math.Max(100, (int)(monitorHeight * MaxHeightRatio) - reserved);

        networksScroll.SetMaxContentHeight(maxListHeight);
    }
}
