using OvraelShell.Models;
using OvraelShell.Models.Calendar;

namespace OvraelShell.Services;

/// <summary>Shows calendar with events.</summary>
public sealed class CalendarService : IDisposable
{
    ReactiveCollection<CalendarEvent> events = new();

    public ReactiveCollection<CalendarEvent> TodayEvents { get; set; } = new();
    public ReactiveProperty<bool> AnyEventsToday = new();

    public CalendarService()
    {
        events.Changed += (items) => TodayEvents.Set(items.Where(i => i.IsToday()));
        TodayEvents.Changed += (items) => AnyEventsToday.Set(items.Any());
        System.Console.WriteLine($"events {events.Count}");
    }

    public int GetDayEventsCount(DateOnly date)
    {
        return events.Items.Count(i => i.IsDay(date));
    }

    public void Dispose() { }

    public void AddEvent(CalendarEvent calendarEvent)
    {
        if (events.Items.Contains(calendarEvent))
        {
            return;
        }

        events.Add(calendarEvent);
    }
}
