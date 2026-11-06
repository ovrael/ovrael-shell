using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;
using OvraelShell.Models.Calendar;
using OvraelShell.Services;

namespace OvraelShell.Widgets.Calendar.Elements;

public class CreateEventFormData
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public bool IsAllDay { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    private List<string>? Validate()
    {
        List<string>? errors = [];

        string? titleError = ValidateTitle(Title);
        if (titleError is not null)
            errors.Add(titleError);

        string? dateTimeError = ValidateDateTime(Start, End, IsAllDay);
        if (dateTimeError is not null)
            errors.Add(dateTimeError);

        return errors;
    }

    private string? ValidateTitle(string title)
    {
        if (string.IsNullOrEmpty(title))
            return "Title cannot be empty";
        return null;
    }

    private string? ValidateDateTime(DateTime start, DateTime end, bool isAllDay)
    {
        if (isAllDay)
            return null;

        if (DateTime.Compare(start, end) > 0)
            return "Start cannot be later than end";
        return null;
    }
}

[GObject.Subclass<Gtk.Box>]
public sealed partial class CalendarDay
{
    Button button;
    DateOnly date;
    CalendarService service;

    public static CalendarDay New(
        CalendarService service,
        DateOnly date,
        IEnumerable<string> cssClasses,
        bool inCurrentMonth
    )
    {
        var day = NewWithProperties([]);
        day.date = date;
        day.service = service;
        day.CreateButton($"{date.Day}", cssClasses, inCurrentMonth);
        return day;
    }

    partial void Initialize()
    {
        SetOrientation(Orientation.Horizontal);
        SetSpacing(4);
        AddCssClass("calendar-day-box");
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

        System.Console.WriteLine(
            $"Create day button date: {date} in current month? {inCurrentMonth}"
        );

        if (inCurrentMonth)
        {
            button.SetCursor(Cursors.Pointer);
            button.Focusable = true;
            button.OnClicked += CreateEvent;
        }

        button.TooltipText =
            $"In the day {date} there are {service.GetDayEventsCount(date)} events!";

        Append(button);
    }

    private void CreateEvent(Button b, EventArgs e)
    {
        service.AddEvent(
            new CalendarEvent()
            {
                Name = "Testowy",
                Description = "Testowy bardzo prosty event",
                Start = DateTime.Now,
                End = DateTime.Now.AddHours(1),
            }
        );

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
