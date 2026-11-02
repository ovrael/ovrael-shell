using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;

namespace OvraelShell.Widgets.Calendar.Elements;

[GObject.Subclass<Gtk.Box>]
public sealed partial class CalendarDay
{
    Button button;

    public static CalendarDay New(string label, IEnumerable<string> cssClasses, bool inCurrentMonth)
    {
        var day = NewWithProperties([]);
        day.CreateButton(label, cssClasses, inCurrentMonth);
        return day;
    }

    partial void Initialize()
    {
        SetOrientation(Orientation.Horizontal);
        SetSpacing(4);
        AddCssClass("calendar-day-box");
    }

    [MemberNotNull(nameof(button))]
    private void CreateButton(string label, IEnumerable<string> cssClasses, bool inCurrentMonth)
    {
        button = Button.NewWithLabel(label);
        // Fills the grid cell, so all the days have the same size
        button.SetHexpand(true);
        button.SetVexpand(true);
        foreach (var cssClass in cssClasses)
            button.AddCssClass(cssClass);

        button.Focusable = false;
        if (inCurrentMonth)
        {
            button.SetCursor(Cursors.Pointer);
            button.Focusable = true;
            button.OnClicked += CreateEvent;
        }

        Append(button);
    }

    private void CreateEvent(Button b, EventArgs e)
    {
        button.AddCssClass("calendar-day-event");
    }
}
