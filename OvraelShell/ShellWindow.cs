using OvraelShell.Widgets;

namespace OvraelShell;

[GObject.Subclass<Gtk.ApplicationWindow>]
public partial class ShellWindow
{
    private const int barWidth = 3440;
    private const int barHeight = 50;

    public static new ShellWindow New()
    {
        return NewWithProperties([]);
    }

    // Initializer for the WorkspaceWidget class.
    // This method sets up the window and its contents.
    // Gtk.Application is passed in so we can access the application in this app
    // we will use the reference to the Gtk.Application to quit from a button.
    partial void Initialize()
    {
        Title = "OvraelShell";
        SetDefaultSize(barWidth, barHeight);

        var left = WorkspaceWidget.New();
        var center = ClockWidget.New();
        var right = Gtk.Box.New(Gtk.Orientation.Horizontal, 0);

        var layout = Gtk.CenterBox.New();

        layout.SetStartWidget(left);
        layout.SetCenterWidget(center);
        layout.SetEndWidget(right);

        SetChild(layout);
    }
}
