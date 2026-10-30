public static class CalendarUtils
{
    public static int GetMonthLength(int month, int year)
    {
        month = (month % 12) + 1;

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
}
