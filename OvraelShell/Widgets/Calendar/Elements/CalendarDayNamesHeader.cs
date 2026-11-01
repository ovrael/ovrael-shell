using Gtk;

namespace OvraelShell.Widgets.Calendar.Elements;

[GObject.Subclass<Gtk.Box>]
public sealed partial class CalendarDayNamesHeader
{
    private readonly string[] names = ["Pn", "Wt", "Śr", "Cz", "Pt", "Sb", "Nd"];

    public static CalendarDayNamesHeader New()
    {
        return NewWithProperties([]);
    }

    partial void Initialize()
    {
        SetOrientation(Orientation.Horizontal);
        SetSpacing(4);
        SetHexpand(true);
        AddCssClass("calendar-day-names-header");

        foreach (var name in names)
        {
            var nameBox = Box.New(Orientation.Horizontal, 0);
            nameBox.SetHexpand(true);
            nameBox.SetHalign(Align.Center);
            nameBox.AddCssClass("calendar-day-names-label");

            var nameLabel = Label.New(name);
            nameLabel.SetHexpand(true);
            nameLabel.SetJustify(Justification.Center);

            nameBox.Append(nameLabel);
            Append(nameBox);
        }
    }
}
