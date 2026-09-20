using Gtk;
using OvraelShell.Services;
using OvraelShell.Widgets;

namespace OvraelShell;

[GObject.Subclass<Gtk.ApplicationWindow>]
public partial class MainBar
{
    private readonly NetworkService networkService = new();

    public static MainBar New(int width = 1920, int height = 30)
    {
        var bar = NewWithProperties([]);
        bar.SetDefaultSize(width, height);
        bar.AddCssClass("main-bar");
        return bar;
    }

    partial void Initialize()
    {
        Title = "OvraelShell";

        var layout = Gtk.CenterBox.New();

        CreateStart(layout);
        CreateCenter(layout);
        CreateEnd(layout);

        SetChild(layout);
    }

    private void CreateStart(CenterBox layout)
    {
        var left = Gtk.Box.New(Orientation.Horizontal, 5);

        var weather = WeatherWidget.New();
        left.Append(weather);

        var workspace = WorkspaceWidget.New();
        left.Append(workspace);

        layout.SetStartWidget(left);
    }

    private void CreateCenter(CenterBox layout)
    {
        var center = Gtk.Box.New(Orientation.Horizontal, 5);

        var calendar = CalendarWidget.New();
        center.Append(calendar);

        layout.SetCenterWidget(center);
    }

    private void CreateEnd(CenterBox layout)
    {
        var end = Gtk.Box.New(Orientation.Horizontal, 5);

        var bluetooth = BluetoothWidget.New();
        end.Append(bluetooth);

        var audio = AudioWidget.New();
        end.Append(audio);

        var internet = InternetWidget.New(networkService);
        end.Append(internet);

        var themeChanger = ThemeChangerWidget.New();
        end.Append(themeChanger);

        layout.SetEndWidget(end);
    }
}
