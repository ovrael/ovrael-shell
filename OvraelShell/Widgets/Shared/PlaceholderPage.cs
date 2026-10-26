using Gtk;

namespace OvraelShell.Widgets.Shared;

/// <summary>
/// Content of a popover whose widget is not implemented yet - a title and the plan.
/// Remove it together with the last widget that uses it.
/// </summary>
public static class PlaceholderPage
{
    // The plan is long - wrapped, so the popover does not take the whole width of the monitor
    private const int MaxWidthChars = 60;

    public static Box Create(string title, string plan)
    {
        var page = Box.New(Orientation.Vertical, 8);
        page.SetMarginTop(12);
        page.SetMarginBottom(12);
        page.SetMarginStart(12);
        page.SetMarginEnd(12);

        var titleLabel = Label.New(title);
        titleLabel.SetHalign(Align.Start);
        titleLabel.AddCssClass("placeholder-title");
        page.Append(titleLabel);

        var planLabel = Label.New($"Not implemented yet. {plan}");
        planLabel.SetHalign(Align.Start);
        planLabel.SetXalign(0);
        planLabel.SetWrap(true);
        planLabel.SetMaxWidthChars(MaxWidthChars);
        planLabel.AddCssClass("placeholder-text");
        page.Append(planLabel);

        return page;
    }
}
