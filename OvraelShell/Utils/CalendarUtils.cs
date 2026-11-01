public static class CalendarUtils
{
    private static readonly string[] monthNames =
    [
        "Styczeń",
        "Luty",
        "Marzec",
        "Kwiecień",
        "Maj",
        "Czerwiec",
        "Lipiec",
        "Sierpień",
        "Wrzesień",
        "Październik",
        "Listopad",
        "Grudzień",
    ];

    public static int GetMonthLength(int month, int year)
    {
        return month switch
        {
            4 or 6 or 9 or 11 => 30,
            2 => 28 + ((year % 4 == 0 && year % 100 != 0) || year % 400 == 0 ? 1 : 0),
            _ => 31,
        };
    }

    public static string CreateDate(int day, int month, int year)
    {
        return $"{day}-{month}-{year}";
    }

    public static string NumberToMonthName(int month)
    {
        if (month >= 1 && month <= 12)
            return monthNames[month - 1];
        return $"WRONG: {month}";
    }

    public static int MaxMonthNameLength => monthNames.MaxBy(m => m.Length)!.Length;
}
