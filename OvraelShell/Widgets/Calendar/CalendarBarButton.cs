using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.Enums.Settings;
using OvraelShell.InterfaceElements;
using OvraelShell.Models;
using OvraelShell.Models.Calendar;
using OvraelShell.Services;
using OvraelShell.Utils.Settings;

namespace OvraelShell.Widgets.Calendar;

[GObject.Subclass<Gtk.Box>]
public sealed partial class CalendarBarButton : IWithDisposableService<CalendarService>
{
    private Label spaceLabel;
    private Label timeLabel;
    private Label eventLabel;
    private Button button;
    public CalendarService Service { get; private set; }
    private CalendarPopover popover;
    private uint timeoutId;

    readonly ReactiveCollection<CalendarEvent> todayEvents = new();

    public static CalendarBarButton New(CalendarService service)
    {
        var widget = NewWithProperties([]);
        widget.AddService(service);
        widget.InitPopover(service);
        widget.SetupEvents(service);
        return widget;
    }

    private void SetupEvents(CalendarService service)
    {
        service.Events.Changed += (_) =>
        {
            var events = service.GetDayEvents(DateOnly.FromDateTime(DateTime.Now));
            todayEvents.SetIfChanged(events);
        };

        todayEvents.Changed += (items) =>
        {
            Update();
        };
    }

    [MemberNotNull(nameof(button))]
    partial void Initialize()
    {
        SetOrientation(Orientation.Horizontal);
        button = Button.NewWithLabel(Icons.Clipboard.Default);
        button.SetChild(CreateButtonContent());
        button.SetCursor(Cursors.Pointer);
        button.SetTooltipText("Clipboard history");
        Append(button);
        timeoutId = GLib.Functions.TimeoutAdd(
            0,
            1000,
            () =>
            {
                Update();
                return true;
            }
        );

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

    [MemberNotNull(nameof(timeLabel), nameof(eventLabel))]
    private Box CreateButtonContent()
    {
        Box box = Box.New(Orientation.Horizontal, 6);

        var now = DateTime.Now;

        spaceLabel = Label.New(string.Empty);
        spaceLabel.WidthChars = 3;
        timeLabel = Label.New(now.ToString(UserSettings.General.TimeFormatString));
        timeLabel.WidthChars = 10;
        eventLabel = Label.New(Icons.Calendar.CalendarView);
        eventLabel.WidthChars = 3;

        box.Append(spaceLabel);
        box.Append(timeLabel);
        box.Append(eventLabel);

        return box;
    }

    private void Update()
    {
        if (timeLabel is null)
            return;

        var now = DateTime.Now;
        timeLabel.SetText(now.ToString(UserSettings.General.TimeFormatString));
        button.SetTooltipText(now.ToString(UserSettings.General.DateFormatString));

        if (todayEvents.Items.Count > 0)
        {
            spaceLabel.Show();
            eventLabel.Show();
        }
        else
        {
            spaceLabel.Hide();
            eventLabel.Hide();
        }

        return;
    }
}
