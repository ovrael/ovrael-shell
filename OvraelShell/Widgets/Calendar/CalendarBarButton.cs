using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.Enums.Settings;
using OvraelShell.InterfaceElements;
using OvraelShell.Services;
using OvraelShell.Utils.Settings;

namespace OvraelShell.Widgets.Calendar;

[GObject.Subclass<Gtk.Box>]
public sealed partial class CalendarBarButton : IWithDisposableService<CalendarService>
{
    private Button button;
    public CalendarService Service { get; private set; }
    private CalendarPopover popover;
    private uint timeoutId;

    public static CalendarBarButton New(CalendarService service)
    {
        var widget = NewWithProperties([]);
        widget.AddService(service);
        widget.InitPopover(service);
        return widget;
    }

    [MemberNotNull(nameof(button))]
    partial void Initialize()
    {
        SetOrientation(Orientation.Horizontal);
        button = Button.NewWithLabel(Icons.Clipboard.Default);
        button.SetCursor(Cursors.Pointer);
        button.SetTooltipText("Clipboard history");
        Append(button);
        Update();
        timeoutId = GLib.Functions.TimeoutAdd(0, 1000, Update);

        // Settings are static - follow them only while shown, so they do not keep the widget alive
        OnMap += (_, _) =>
        {
            UserSettings.General.TimeFormat.Changed += OnTimeFormatChanged;
            UserSettings.General.DateFormat.Changed += OnDateFormatChanged;
            Update();
        };
        OnUnmap += (_, _) =>
        {
            UserSettings.General.TimeFormat.Changed -= OnTimeFormatChanged;
            UserSettings.General.DateFormat.Changed -= OnDateFormatChanged;
        };

        OnDestroy += (_, _) => GLib.Functions.SourceRemove(timeoutId);
    }

    [MemberNotNull(nameof(Service))]
    public void AddService(CalendarService service)
    {
        Service = service;
    }

    // Safe to call more than once
    public void RemoveService()
    {
        popover.RemoveService();
    }

    [MemberNotNull(nameof(popover))]
    private void InitPopover(CalendarService service)
    {
        popover = CalendarPopover.New(service, button);
        button.OnClicked += (_, _) => popover.Popup();
    }

    private void OnTimeFormatChanged(TimeFormat _) => Update();

    private void OnDateFormatChanged(string _) => Update();

    private bool Update()
    {
        var now = DateTime.Now;
        button.SetLabel(now.ToString(UserSettings.General.TimeFormatString));
        button.SetTooltipText(now.ToString(UserSettings.General.DateFormatString));
        return true;
    }
}
