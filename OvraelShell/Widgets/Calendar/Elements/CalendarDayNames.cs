using Gtk;

[GObject.Subclass<Gtk.Box>]
public sealed partial class CalendarDayNames
{
    public static CalendarDayNames New()
    {
        return NewWithProperties([]);
    }

    partial void Initialize()
    {
        SetOrientation(Orientation.Horizontal);
        SetSpacing(4);
        AddCssClass("calendar-day-names-box");
    }
}
