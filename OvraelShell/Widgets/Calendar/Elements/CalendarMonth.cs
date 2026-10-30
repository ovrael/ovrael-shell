using System.Diagnostics.CodeAnalysis;
using Gtk;

[GObject.Subclass<Gtk.Box>]
public sealed partial class CalendarMonth
{
    Grid daysGrid;

    public static CalendarMonth New(DateOnly date, int monthDifference = 0, int yearDifference = 0)
    {
        var month = NewWithProperties([]);
        month.CreateDays(date, monthDifference, yearDifference);
        return month;
    }

    partial void Initialize()
    {
        SetOrientation(Orientation.Vertical);
        SetSpacing(6);
        AddCssClass("calendar-day-box");
    }

    [MemberNotNull(nameof(daysGrid))]
    private void CreateDays(DateOnly date, int monthDifference, int yearDifference)
    {
        daysGrid = Grid.New();
        daysGrid.RowSpacing = 4;
        daysGrid.ColumnSpacing = 4;
        // Homogeneous - one-digit days get the same size as two-digit days
        daysGrid.ColumnHomogeneous = true;
        daysGrid.RowHomogeneous = true;

        const int WEEKS_COUNT = 6;
        const int DAYS_COUNT = 7;

        var dayOfMonth = date.Day;
        var month = date.Month - monthDifference;
        var year = date.Year + yearDifference;

        var previousMonthLength = CalendarUtils.GetMonthLength((month + 11) % 12, year);
        var currentMonthLength = CalendarUtils.GetMonthLength(month, year);

        var dayCounter = 0;
        var startMonthDay = new DateOnly(year, month, 1);
        var start = (((int)startMonthDay.DayOfWeek - 1 + 6) % 7) + 1; // DayOfWeek: 0..6 (0 = sunday)

        // Previous month
        for (int i = 0; i < start; i++)
        {
            var day = CalendarDay.New(
                $"{previousMonthLength - start + i + 1}",
                ["calendar-day", "calendar-day-previous-month"],
                false
            );
            daysGrid.Attach(day, dayCounter % DAYS_COUNT, dayCounter / DAYS_COUNT, 1, 1);
            dayCounter++;
        }

        // Current month
        for (int i = 0; i < currentMonthLength; i++)
        {
            var eventDate = CalendarUtils.CreateDate(i + 1, month, year);

            List<string> dayCssClasses = ["calendar-day-current-month"];
            if (i + 1 == dayOfMonth && monthDifference % 12 == 0 && yearDifference == 0)
                dayCssClasses = ["calendar-day-current-month-day"];

            //   if (events.some((e) => e.date == date)) {
            //     dayCssClasses.push("calendar-day-event");
            //   }

            dayCssClasses.Add("calendar-day");

            var day = CalendarDay.New($"{i + 1}", dayCssClasses, true);
            daysGrid.Attach(day, dayCounter % DAYS_COUNT, dayCounter / DAYS_COUNT, 1, 1);
            dayCounter++;
        }

        // Next month
        int nextMonthDaysCount = WEEKS_COUNT * DAYS_COUNT - dayCounter;
        for (int i = 0; i < nextMonthDaysCount; i++)
        {
            var day = CalendarDay.New(
                $"{i + 1}",
                ["calendar-day", "calendar-day-next-month"],
                false
            );
            daysGrid.Attach(day, dayCounter % DAYS_COUNT, dayCounter / DAYS_COUNT, 1, 1);
            dayCounter++;
        }

        Append(daysGrid);
    }
}
