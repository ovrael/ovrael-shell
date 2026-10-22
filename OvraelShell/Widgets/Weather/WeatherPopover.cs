using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Gtk;
using OvraelShell.InterfaceElements;
using OvraelShell.Models.Weather;
using OvraelShell.Services;
using OvraelShell.Utils;
using OvraelShell.Widgets.Weather.Elements;

namespace OvraelShell.Widgets.Weather;

[GObject.Subclass<Gtk.Popover>]
public sealed partial class WeatherPopover : IWithDisposableService<WeatherService>
{
    // Content margins (2 x 12) plus .popover contents padding and border (2 x 10 + 2 x 1)
    private const int PopoverChrome = 24 + 22;

    // Free space between the popover and the edges of the monitor
    private const int MonitorMargin = 2 * 8;

    public WeatherService Service { get; private set; }

    private Box content;
    private Stack pages;
    private StackSwitcher tabs;
    private ScrolledWindow tabsScroll;

    // One tab for each location of the service, in its order
    private readonly Dictionary<WeatherForecast, LocationTab> locationTabs = [];

    public static WeatherPopover New(WeatherService service, Button parent)
    {
        var widget = NewWithProperties([]);
        widget.AddService(service);
        widget.SetParent(parent);
        return widget;
    }

    [MemberNotNull(nameof(content), nameof(pages), nameof(tabs), nameof(tabsScroll))]
    partial void Initialize()
    {
        CreateContent();

        SetPosition(PositionType.Top);
        SetAutohide(true);
        SetChild(content);
        AddCssClass("popover");

        OnShow += (_, _) => UpdateTabsWidth();
    }

    [MemberNotNull(nameof(Service))]
    public void AddService(WeatherService service)
    {
        Service = service;

        Service.Locations.Added += AddLocation;
        Service.Locations.Removed += RemoveLocation;
        Service.Locations.Cleared += ClearLocations;
        Service.MainLocation.Changed += ShowLocation;

        foreach (var location in Service.Locations.Items)
            AddLocation(location);

        ShowLocation(Service.MainLocation.Value);
    }

    // Safe to call more than once
    public void RemoveService()
    {
        Service.Locations.Added -= AddLocation;
        Service.Locations.Removed -= RemoveLocation;
        Service.Locations.Cleared -= ClearLocations;
        Service.MainLocation.Changed -= ShowLocation;

        ClearLocations();
    }

    [MemberNotNull(nameof(content), nameof(pages), nameof(tabs), nameof(tabsScroll))]
    private void CreateContent()
    {
        content = Box.New(Orientation.Vertical, 8);
        content.SetMarginTop(12);
        content.SetMarginBottom(12);
        content.SetMarginStart(12);
        content.SetMarginEnd(12);

        pages = Stack.New();

        tabs = StackSwitcher.New();
        tabs.SetStack(pages);
        tabs.SetHalign(Align.Center);
        tabs.AddCssClass("weather-tabs");

        // The tab buttons are created by the StackSwitcher - they take the cursor from it
        tabs.SetCursor(Cursors.Pointer);

        // The popover is as wide as all the tabs - only when the monitor is too narrow,
        // the tabs scroll (see UpdateTabsWidth) and the pages keep their place
        tabsScroll = ScrolledWindow.New();
        tabsScroll.SetPolicy(PolicyType.Automatic, PolicyType.Never);
        tabsScroll.SetPropagateNaturalWidth(true);
        tabsScroll.SetPropagateNaturalHeight(true);
        tabsScroll.SetChild(tabs);

        HorizontalScroll.AddMouseWheel(tabsScroll);

        content.Append(tabsScroll);
        content.Append(pages);
    }

    #region Location tabs

    /// <summary>A tab and the handler that keeps its title equal to the location name.</summary>
    private sealed record LocationTab(WeatherLocationPage Page, Action<string> OnNameChanged);

    private void AddLocation(WeatherForecast location)
    {
        if (locationTabs.ContainsKey(location))
            return;

        var page = new WeatherLocationPage(location);

        // The coordinates are unique in the service, the name can be empty or repeated
        var name = string.Create(
            CultureInfo.InvariantCulture,
            $"{location.Latitude},{location.Longitude}"
        );
        var stackPage = pages.AddTitled(page.View, name, TitleOf(location));

        // The name can come later, from the reverse geocoding
        var tab = new LocationTab(page, _ => stackPage.SetTitle(TitleOf(location)));
        location.Name.Changed += tab.OnNameChanged;
        locationTabs[location] = tab;
    }

    private void RemoveLocation(WeatherForecast location)
    {
        if (!locationTabs.Remove(location, out var tab))
            return;

        location.Name.Changed -= tab.OnNameChanged;
        tab.Page.Dispose();
        pages.Remove(tab.Page.View);
    }

    private void ClearLocations()
    {
        foreach (var location in locationTabs.Keys.ToArray())
            RemoveLocation(location);
    }

    private void ShowLocation(WeatherForecast? location)
    {
        if (location is not null && locationTabs.TryGetValue(location, out var tab))
            pages.SetVisibleChild(tab.Page.View);
    }

    // The service gives a name to each location - "Location #n" when it has none
    private static string TitleOf(WeatherForecast location) => location.Name.Value;

    #endregion

    /// <summary>
    /// Limits the tabs to the width of the monitor the bar is on. Narrower tabs keep their
    /// natural width, wider ones scroll. Done on every show - the bar can move between monitors.
    /// </summary>
    private void UpdateTabsWidth()
    {
        var monitorWidth = GetParent() is { } parent ? MonitorSize.WidthOf(parent) : 0;

        // Before the bar is on a monitor - no limit
        tabsScroll.SetMaxContentWidth(
            monitorWidth > 0 ? Math.Max(0, monitorWidth - PopoverChrome - MonitorMargin) : -1
        );
    }
}
