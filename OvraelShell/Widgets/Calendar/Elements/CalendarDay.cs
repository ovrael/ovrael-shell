using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;

[GObject.Subclass<Gtk.Box>]
public sealed partial class CalendarDay
{
    Button button;

    [MemberNotNull(nameof(button))]
    public static CalendarDay New(string label, IEnumerable<string> cssClasses, bool inCurrentMonth)
    {
        var day = NewWithProperties([]);
        day.button = Button.NewWithLabel(label);
        day.button.SetCursor(Cursors.Pointer);
        // Fills the grid cell, so all the days have the same size
        day.button.SetHexpand(true);
        day.button.SetVexpand(true);
        foreach (var cssClass in cssClasses)
            day.button.AddCssClass(cssClass);
        day.Append(day.button);
        return day;
    }

    partial void Initialize()
    {
        SetOrientation(Orientation.Horizontal);
        SetSpacing(4);
        AddCssClass("calendar-day-box");
    }
}
