using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;
using OvraelShell.Models;
using OvraelShell.Services;
using OvraelShell.Widgets.Calendar.Elements;
using OvraelShell.Widgets.Shared;

namespace OvraelShell.Widgets.Calendar;

[GObject.Subclass<Gtk.Popover>]
public sealed partial class CalendarPopover : IWithDisposableService<CalendarService>
{
    public CalendarService Service { get; private set; }

    private Label monthLabel;
    private Label yearLabel;
    private CalendarMonth calendarMonth;

    private readonly ReactiveProperty<DateOnly> date = new(DateOnly.FromDateTime(DateTime.Now));

    private Box content;

    public static CalendarPopover New(CalendarService service, Button parent)
    {
        var widget = NewWithProperties([]);
        widget.AddService(service);
        widget.AppendCalendarWidgets();
        widget.SetParent(parent);
        return widget;
    }

    [MemberNotNull(nameof(content))]
    partial void Initialize()
    {
        content = CreateContentBox();
        SetPosition(PositionType.Top);
        SetAutohide(true);
        SetChild(content);
        AddCssClass("popover");
        OnShow += (_, _) =>
        {
            date.Set(DateOnly.FromDateTime(DateTime.Now));
        };
    }

    private void AppendCalendarWidgets()
    {
        var dateChanger = CreateDateChanger();
        var dayNames = CalendarDayNamesHeader.New();
        calendarMonth = CalendarMonth.New(Service, date);

        content.Append(dateChanger);
        content.Append(dayNames);
        content.Append(calendarMonth);

        date.Changed += (d) =>
        {
            calendarMonth.FillDaysGrid(d);
            UpdateLabels();
        };
    }

    [MemberNotNull(nameof(Service))]
    public void AddService(CalendarService service)
    {
        Service = service;
    }

    // Safe to call more than once
    public void RemoveService() { }

    private Box CreateContentBox()
    {
        var box = Box.New(Orientation.Vertical, 8);
        box.SetMarginTop(12);
        box.SetMarginBottom(12);
        box.SetMarginStart(12);
        box.SetMarginEnd(12);
        return box;
    }

    private void UpdateLabels()
    {
        monthLabel.SetText(CalendarUtils.NumberToMonthName(date.Value.Month));
        yearLabel.SetText($"{date.Value.Year}");
    }

    private Box CreateDateChanger()
    {
        var dateChanger = Box.New(Orientation.Horizontal, 8);
        var date = DateTime.Now;

        var monthChanger = CreateMonthChanger(date.Month);
        dateChanger.Append(monthChanger);

        var yearChanger = CreateYearChanger(date.Year);
        dateChanger.Append(yearChanger);

        return dateChanger;
    }

    [MemberNotNull(nameof(monthLabel))]
    private Box CreateMonthChanger(int currentMonth)
    {
        var container = Box.New(Orientation.Horizontal, 6);
        container.Hexpand = true;
        var previousButton = Button.NewWithLabel("<");
        previousButton.SetCursor(Cursors.Pointer);
        monthLabel = Label.New(CalendarUtils.NumberToMonthName(currentMonth));
        monthLabel.WidthChars = CalendarUtils.MaxMonthNameLength + 2;
        var nextButton = Button.NewWithLabel(">");
        nextButton.SetCursor(Cursors.Pointer);

        previousButton.OnClicked += (_, _) => date.Set(date.Value.AddMonths(-1));
        nextButton.OnClicked += (_, _) => date.Set(date.Value.AddMonths(1));

        container.Append(previousButton);
        container.Append(monthLabel);
        container.Append(nextButton);

        return container;
    }

    [MemberNotNull(nameof(yearLabel))]
    private Box CreateYearChanger(int currentYear)
    {
        var container = Box.New(Orientation.Horizontal, 6);
        var previousButton = Button.NewWithLabel("<");
        previousButton.SetCursor(Cursors.Pointer);
        yearLabel = Label.New($"{currentYear}");
        yearLabel.WidthChars = 8;
        var nextButton = Button.NewWithLabel(">");
        nextButton.SetCursor(Cursors.Pointer);

        previousButton.OnClicked += (_, _) => date.Set(date.Value.AddYears(-1));
        nextButton.OnClicked += (_, _) => date.Set(date.Value.AddYears(1));

        container.Append(previousButton);
        container.Append(yearLabel);
        container.Append(nextButton);

        return container;
    }
}
