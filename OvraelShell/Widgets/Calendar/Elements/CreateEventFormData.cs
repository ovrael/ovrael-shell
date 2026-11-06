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
