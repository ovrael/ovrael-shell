namespace OvraelShell.Models.Calendar;

public class CalendarEvent
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    public bool IsToday()
    {
        return IsDay(DateOnly.FromDateTime(DateTime.Today));
    }

    public bool IsDay(DateOnly day)
    {
        var start = DateOnly.FromDateTime(Start);
        var end = DateOnly.FromDateTime(End);

        return start == day || end == day;
    }

    public bool Equals(CalendarEvent? other) =>
        other is not null && Name == other.Name && Start == other.Start && End == other.End;

    public override bool Equals(object? obj) => Equals(obj as CalendarEvent);
}
