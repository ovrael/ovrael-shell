using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.Services;
using OvraelShell.Widgets.Audio;
using OvraelShell.Widgets.Bluetooth;
using OvraelShell.Widgets.Clipboard;
using OvraelShell.Widgets.KdeConnect;
using OvraelShell.Widgets.Media;
using OvraelShell.Widgets.Network;
using OvraelShell.Widgets.Notifications;
using OvraelShell.Widgets.Obsidian;
using OvraelShell.Widgets.Simple;
using OvraelShell.Widgets.SystemStatus;
using OvraelShell.Widgets.Trash;
using OvraelShell.Widgets.Tray;
using OvraelShell.Widgets.Weather;

namespace OvraelShell.Widgets;

[GObject.Subclass<Gtk.ApplicationWindow>]
public partial class MainBar
{
    // Shared by all bars and owned by Program - it outlives this one
    private ShellServices services;

    private NetworkBarButton networkButton;
    private BluetoothBarButton bluetoothButton;
    private AudioBarButton audioButton;
    private WeatherBarButton weatherButton;
    private SystemStatusBarButton systemStatusButton;
    private ObsidianBarButton obsidianButton;
    private ClipboardBarButton clipboardButton;
    private MediaBarButton mediaButton;
    private NotificationsBarButton notificationsButton;
    private TrayBarButton trayButton;
    private KdeConnectBarButton kdeConnectButton;
    private TrashBarButton trashButton;

    public static MainBar New(ShellServices services, int width = 1920, int height = 30)
    {
        var bar = NewWithProperties([]);
        bar.services = services;
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
            RemoveServices();
            return false;
        };
    }

    /// <summary>
    /// Detaches the bar from the shared services. Called on close; call it yourself before
    /// destroying the bar in another way (Destroy, application quit) - those skip close-request.
    /// Safe to call more than once.
    /// </summary>
    public void RemoveServices()
    {
        networkButton?.RemoveService();
        bluetoothButton?.RemoveService();
        audioButton?.RemoveService();
        weatherButton?.RemoveService();
        systemStatusButton?.RemoveService();
        obsidianButton?.RemoveService();
        clipboardButton?.RemoveService();
        mediaButton?.RemoveService();
        notificationsButton?.RemoveService();
        trayButton?.RemoveService();
        kdeConnectButton?.RemoveService();
        trashButton?.RemoveService();
    }

    // Widgets that need the service are created after New has set it
    [MemberNotNull(
        nameof(networkButton),
        nameof(bluetoothButton),
        nameof(audioButton),
        nameof(weatherButton),
        nameof(systemStatusButton),
        nameof(obsidianButton),
        nameof(clipboardButton),
        nameof(mediaButton),
        nameof(notificationsButton),
        nameof(trayButton),
        nameof(kdeConnectButton),
        nameof(trashButton)
    )]
    private void CreateLayout()
    {
        var layout = Gtk.CenterBox.New();

        CreateStart(layout);
        CreateCenter(layout);
        CreateEnd(layout);

        SetChild(layout);
    }

    [MemberNotNull(nameof(weatherButton), nameof(systemStatusButton))]
    private void CreateStart(CenterBox layout)
    {
        var left = Gtk.Box.New(Orientation.Horizontal, 5);

        weatherButton = WeatherBarButton.New(services.Weather);
        left.Append(weatherButton);

        var caffeine = CaffeineWidget.New();
        left.Append(caffeine);

        var workspace = WorkspaceWidget.New(services.Hyprland);
        left.Append(workspace);

        systemStatusButton = SystemStatusBarButton.New(services.SystemStatus);
        left.Append(systemStatusButton);

        layout.SetStartWidget(left);
    }

    [MemberNotNull(nameof(mediaButton), nameof(obsidianButton))]
    private void CreateCenter(CenterBox layout)
    {
        var center = Gtk.Box.New(Orientation.Horizontal, 5);

        mediaButton = MediaBarButton.New(services.Media);
        center.Append(mediaButton);

        var calendar = CalendarWidget.New();
        center.Append(calendar);

        // Next to the date - the note is the note of this day
        obsidianButton = ObsidianBarButton.New(services.Obsidian);
        center.Append(obsidianButton);

        layout.SetCenterWidget(center);
    }

    [MemberNotNull(
        nameof(networkButton),
        nameof(bluetoothButton),
        nameof(audioButton),
        nameof(clipboardButton),
        nameof(notificationsButton),
        nameof(trayButton),
        nameof(kdeConnectButton),
        nameof(trashButton)
    )]
    private void CreateEnd(CenterBox layout)
    {
        var end = Gtk.Box.New(Orientation.Horizontal, 5);

        trayButton = TrayBarButton.New(services.Tray);
        end.Append(trayButton);

        kdeConnectButton = KdeConnectBarButton.New(services.KdeConnect);
        end.Append(kdeConnectButton);

        bluetoothButton = BluetoothBarButton.New(services.Bluetooth);
        end.Append(bluetoothButton);

        audioButton = AudioBarButton.New(services.Audio);
        end.Append(audioButton);

        networkButton = NetworkBarButton.New(services.Network);
        end.Append(networkButton);

        clipboardButton = ClipboardBarButton.New(services.Clipboard);
        end.Append(clipboardButton);

        trashButton = TrashBarButton.New(services.Trash);
        end.Append(trashButton);

        var themeChanger = ThemeChangerWidget.New();
        end.Append(themeChanger);

        // Last, at the edge of the screen - where the notification popups will show
        notificationsButton = NotificationsBarButton.New(services.Notifications);
        end.Append(notificationsButton);

        layout.SetEndWidget(end);
    }
}
