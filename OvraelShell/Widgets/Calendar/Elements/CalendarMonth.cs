using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.Services;

namespace OvraelShell.Widgets.Calendar.Elements;

[GObject.Subclass<Gtk.Box>]
public sealed partial class CalendarMonth
{
    Grid daysGrid;
    CalendarService service;

    public static CalendarMonth New(
        CalendarService service,
        DateOnly dateOnly
    // int monthDifference = 0,
    // int yearDifference = 0
    )
    {
        var month = NewWithProperties([]);
        month.service = service;
        month.FillDaysGrid(dateOnly);
        return month;
    }

    [MemberNotNull(nameof(daysGrid))]
    partial void Initialize()
    {
        SetOrientation(Orientation.Vertical);
        SetSpacing(6);
        AddCssClass("calendar-day-box");

        daysGrid = Grid.New();
        daysGrid.RowSpacing = 4;
        daysGrid.ColumnSpacing = 4;
        // Homogeneous - one-digit days get the same size as two-digit days
        daysGrid.ColumnHomogeneous = true;
        daysGrid.RowHomogeneous = true;
        Append(daysGrid);
    }

    private void ClearGrid()
    {
        var day = daysGrid.GetFirstChild();
        while (day != null)
        {
            daysGrid.Remove(day);
            day.Dispose();
            day = daysGrid.GetFirstChild();
        }
    }

    public void FillDaysGrid(DateOnly date)
    {
        ClearGrid();
        const int WEEKS_COUNT = 6;
        const int DAYS_COUNT = 7;
        int dayCounter = 0;

        // Set start to monday (current or previous month)
        var startDate = date.AddDays(-date.Day);
        var daysToNextMonth = (int)startDate.DayOfWeek - 1;
        startDate = startDate.AddDays(-daysToNextMonth);

        // Previous month
        for (int i = 0; i < daysToNextMonth + 1; i++)
        {
            var day = CalendarDay.New(
                service,
                startDate,
                ["calendar-day", "calendar-day-previous-month"],
                false
            );
            daysGrid.Attach(day, dayCounter % DAYS_COUNT, dayCounter / DAYS_COUNT, 1, 1);
            startDate = startDate.AddDays(1);
            dayCounter++;
        }

        // Current month
        var currentDayDate = DateOnly.FromDateTime(DateTime.Now);
        int currentMonth = startDate.Month;
        do
        {
            List<string> dayCssClasses = ["calendar-day", "calendar-day-current-month"];
            if (startDate == currentDayDate)
                dayCssClasses = ["calendar-day-current-month-today"];

            var day = CalendarDay.New(service, startDate, dayCssClasses, true);
            daysGrid.Attach(day, dayCounter % DAYS_COUNT, dayCounter / DAYS_COUNT, 1, 1);
            startDate = startDate.AddDays(1);
            dayCounter++;
        } while (currentMonth == startDate.Month);

        // Next month
        int nextMonthDaysCount = WEEKS_COUNT * DAYS_COUNT - dayCounter;
        for (int i = 0; i < nextMonthDaysCount; i++)
        {
            var day = CalendarDay.New(
                service,
                startDate,
                ["calendar-day", "calendar-day-next-month"],
                false
            );
            daysGrid.Attach(day, dayCounter % DAYS_COUNT, dayCounter / DAYS_COUNT, 1, 1);
            startDate = startDate.AddDays(1);
            dayCounter++;
        }
    }
}
