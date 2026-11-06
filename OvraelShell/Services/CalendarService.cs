using OvraelShell.Models;
using OvraelShell.Models.Calendar;

namespace OvraelShell.Services;

/// <summary>Shows calendar with events.</summary>
public sealed class CalendarService : IDisposable
{
    public ReactiveCollection<CalendarEvent> Events { get; private set; } = new();

    public CalendarService()
    {
        System.Console.WriteLine($"events {Events.Count}");
    }

    public IEnumerable<CalendarEvent> GetDayEvents(DateOnly date)
    {
        return Events.Items.Where(i => i.IsDay(date));
    }

    public void Dispose() { }

    public bool TryAddEvent(CalendarEvent calendarEvent)
    {
        if (Events.Items.Contains(calendarEvent))
        {
            System.Console.WriteLine($"Calendar contains exact event (name, startTime, endTime)");
            return false;
        }

        Events.Add(calendarEvent);
        return true;
    }
}
