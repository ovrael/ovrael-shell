using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;
using OvraelShell.Models;
using OvraelShell.Models.Calendar;
using OvraelShell.Services;

namespace OvraelShell.Widgets.Calendar.Elements;

[GObject.Subclass<Gtk.Box>]
public sealed partial class CalendarDay
{
    Button button;
    DateOnly date;
    CalendarService service;
    readonly ReactiveCollection<CalendarEvent> events = new();

    public static CalendarDay New(
        CalendarService service,
        DateOnly date,
        IEnumerable<string> cssClasses,
        bool inCurrentMonth
    )
    {
        var day = NewWithProperties([]);
        day.AssignProperties(service, date);
        day.CreateButton($"{date.Day}", cssClasses, inCurrentMonth);
        day.SetupEvents(day.service);
        return day;
    }

    partial void Initialize()
    {
        SetOrientation(Orientation.Horizontal);
        SetSpacing(4);
        AddCssClass("calendar-day-box");
    }

    private void AssignProperties(CalendarService calendarService, DateOnly date)
    {
        service = calendarService;
        this.date = date;
    }

    private void SetupEvents(CalendarService service)
    {
        service.Events.Changed += (_) =>
        {
            var dayEvents = service.GetDayEvents(date).ToList();
            events.SetIfChanged(dayEvents);
        };

        events.Changed += (items) =>
        {
            System.Console.WriteLine(
                $"Events changed for date: {date}, items count: {items.Count}"
            );
            button.TooltipText = $"For date {date} there are {items.Count} events!";
        };

        events.Set(service.GetDayEvents(date).ToList());
    }

    [MemberNotNull(nameof(button))]
    private void CreateButton(string label, IEnumerable<string> cssClasses, bool inCurrentMonth)
    {
        button = Button.NewWithLabel(label);
        // Fills the grid cell, so all the days have the same size
        button.SetHexpand(true);
        button.SetVexpand(true);
        foreach (var cssClass in cssClasses)
            button.AddCssClass(cssClass);

        button.Focusable = false;
        button.TooltipText = $"For date {date} there are {events.Items.Count} events!";

        if (inCurrentMonth)
        {
            button.SetCursor(Cursors.Pointer);
            button.Focusable = true;
            button.OnClicked += CreateEvent;
        }

        Append(button);
    }

    private void CreateEvent(Button b, EventArgs e)
    {
        var dateTime = date.ToDateTime(new TimeOnly());
        bool added = service.TryAddEvent(
            new CalendarEvent()
            {
                Name = "Testowy",
                Description = "Testowy bardzo prosty event",
                Start = dateTime,
                End = dateTime.AddHours(Random.Shared.Next(23) + 1),
            }
        );

        if (added)
            System.Console.WriteLine($"Added event for {date}");

        // CreateEventFormData form = new CreateEventFormData();
        // button.AddCssClass("calendar-day-event");

        // var window = Window.New();
        // window.Title = "Create event";

        // var contentBox = Box.New(Orientation.Vertical, 20);
        // var titleBox = CreateEventTitle(form);
        // contentBox.Append(titleBox);

        // window.SetChild(contentBox);

        // window.Present();
        // window.ForceFloating();
        // window.Resizable = false;
        // window.DefaultHeight = 600;
        // window.DefaultWidth = 300;
        // window.Application = ShellServices.app;
    }

    private Box CreateEventTitle(CreateEventFormData form)
    {
        var box = Box.New(Orientation.Vertical, 4);
        var label = Label.New("Event title");
        var entry = Entry.New();

        box.Append(label);
        box.Append(entry);

        return box;
    }
}
