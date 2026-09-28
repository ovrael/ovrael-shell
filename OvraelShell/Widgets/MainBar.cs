using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.Services;
using OvraelShell.Widgets.Audio;
using OvraelShell.Widgets.Network;

namespace OvraelShell.Widgets;

[GObject.Subclass<Gtk.ApplicationWindow>]
public partial class MainBar
{
    // Shared by all bars and owned by Program - it outlives this one
    private AudioService audioService;
    private NetworkService networkService;

    private NetworkBarButton networkButton;

    public static MainBar New(
        NetworkService networkService,
        AudioService audioService,
        int width = 1920,
        int height = 30
    )
    {
        var bar = NewWithProperties([]);
        bar.networkService = networkService;
        bar.audioService = audioService;
        bar.CreateLayout();
        bar.SetDefaultSize(width, height);
        bar.AddCssClass("main-bar");
        return bar;
    }

    partial void Initialize()
    {
        Title = "OvraelShell";

        // Also when the compositor closes the layer surface (e.g. the monitor is unplugged).
        // Not left to OnDestroy - the service holds the widgets through its events,
        // so they would never be freed and destroyed.
        OnCloseRequest += (_, _) =>
        {
            RemoveNetworkService();
            return false;
        };
    }

    /// <summary>
    /// Detaches the bar from the shared service. Called on close; call it yourself before
    /// destroying the bar in another way (Destroy, application quit) - those skip close-request.
    /// Safe to call more than once.
    /// </summary>
    public void RemoveNetworkService() => networkButton?.RemoveService();

    // Widgets that need the service are created after New has set it
    [MemberNotNull(nameof(networkButton))]
    private void CreateLayout()
    {
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

    [MemberNotNull(nameof(networkButton))]
    private void CreateEnd(CenterBox layout)
    {
        var end = Gtk.Box.New(Orientation.Horizontal, 5);

        var bluetooth = BluetoothWidget.New();
        end.Append(bluetooth);

        var audio = AudioBarButton.New(audioService);
        end.Append(audio);

        networkButton = NetworkBarButton.New(networkService);
        end.Append(networkButton);

        var themeChanger = ThemeChangerWidget.New();
        end.Append(themeChanger);

        layout.SetEndWidget(end);
    }
}
