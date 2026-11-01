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

    private readonly ReactiveProperty<int> monthDifference = new(0);
    private readonly ReactiveProperty<int> yearDifference = new(0);

    private Box content;

    public static CalendarPopover New(CalendarService service, Button parent)
    {
        var widget = NewWithProperties([]);
        widget.AddService(service);
        widget.SetParent(parent);
        return widget;
    }

    [MemberNotNull(nameof(content))]
    partial void Initialize()
    {
        CreateContent();
        SetPosition(PositionType.Top);
        SetAutohide(true);
        SetChild(content);
        AddCssClass("popover");
        OnShow += (_, _) =>
        {
            monthDifference.Set(0);
            yearDifference.Set(0);
        };

        monthDifference.Changed += (d) =>
        {
            calendarMonth.FillDaysGrid(DateTime.Now, d, yearDifference.Value);
            UpdateLabels();
        };
        yearDifference.Changed += (d) =>
        {
            calendarMonth.FillDaysGrid(DateTime.Now, monthDifference.Value, d);
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

    [MemberNotNull(nameof(content))]
    private void CreateContent()
    {
        content = Box.New(Orientation.Vertical, 8);
        content.SetMarginTop(12);
        content.SetMarginBottom(12);
        content.SetMarginStart(12);
        content.SetMarginEnd(12);

        var dateChanger = CreateDateChanger();
        var dayNames = CalendarDayNamesHeader.New();
        calendarMonth = CalendarMonth.New(
            DateTime.Now,
            monthDifference.Value,
            yearDifference.Value
        );

        content.Append(dateChanger);
        content.Append(dayNames);
        content.Append(calendarMonth);
    }

    private void UpdateLabels()
    {
        var date = DateTime.Now;
        int monthIndex = (date.Month + monthDifference - 1) % 12 + 1;
        monthLabel.SetText(CalendarUtils.NumberToMonthName(monthIndex));
        yearLabel.SetText($"{date.Year + yearDifference}");
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

        previousButton.OnClicked += (_, _) =>
            monthDifference.Set((monthDifference.Value + 11) % 12);

        nextButton.OnClicked += (_, _) => monthDifference.Set((monthDifference.Value + 1) % 12);

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

        previousButton.OnClicked += (_, _) => yearDifference.Set(yearDifference.Value - 1);
        nextButton.OnClicked += (_, _) => yearDifference.Set(yearDifference.Value + 1);

        container.Append(previousButton);
        container.Append(yearLabel);
        container.Append(nextButton);

        return container;
    }
}
