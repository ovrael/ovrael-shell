using Gtk;
using OvraelShell.Models.Settings.Widgets;
using OvraelShell.Services;
using OvraelShell.Utils.Settings;
using OvraelShell.Widgets.Audio;
using OvraelShell.Widgets.Bluetooth;
using OvraelShell.Widgets.Calendar;
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

    // Null when the widget is not loaded - see widgetsToLoad in settings.json
    private NetworkBarButton? networkButton;
    private BluetoothBarButton? bluetoothButton;
    private AudioBarButton? audioButton;
    private WeatherBarButton? weatherButton;
    private SystemStatusBarButton? systemStatusButton;
    private ObsidianBarButton? obsidianButton;
    private ClipboardBarButton? clipboardButton;
    private MediaBarButton? mediaButton;
    private NotificationsBarButton? notificationsButton;
    private TrayBarButton? trayButton;
    private KdeConnectBarButton? kdeConnectButton;
    private TrashBarButton? trashButton;
    private CalendarBarButton? calendarButton;

    public static MainBar New(ShellServices services, int width = 1920, int height = 30)
    {
        var bar = NewWithProperties([]);
        bar.services = services;
        bar.CreateLayout();
        bar.SetDefaultSize(width, height);
        bar.AddCssClass("main-bar");

        UserSettings.WidgetsToLoad.Loaded.Changed += bar.OnWidgetsToLoadChanged;
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
    /// Detaches the bar from the shared services and the settings. Called on close; call it yourself
    /// before destroying the bar in another way (Destroy, application quit) - those skip close-request.
    /// Safe to call more than once.
    /// </summary>
    public void RemoveServices()
    {
        UserSettings.WidgetsToLoad.Loaded.Changed -= OnWidgetsToLoadChanged;
        RemoveWidgetServices();
    }

    private void RemoveWidgetServices()
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
        calendarButton?.RemoveService();
    }

    // The whole layout is built again - the widgets keep their order on the bar
    private void OnWidgetsToLoadChanged(IReadOnlyList<string> _)
    {
        RemoveWidgetServices();

        networkButton = null;
        bluetoothButton = null;
        audioButton = null;
        weatherButton = null;
        systemStatusButton = null;
        obsidianButton = null;
        clipboardButton = null;
        mediaButton = null;
        notificationsButton = null;
        trayButton = null;
        kdeConnectButton = null;
        trashButton = null;
        calendarButton = null;

        CreateLayout();
    }

    private static bool IsLoaded(string name) => UserSettings.WidgetsToLoad.IsLoaded(name);

    // Widgets that need the service are created after New has set it
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

        if (IsLoaded(WidgetName.Weather))
        {
            weatherButton = WeatherBarButton.New(services.Weather);
            left.Append(weatherButton);
        }

        if (IsLoaded(WidgetName.Caffeine))
            left.Append(CaffeineWidget.New());

        if (IsLoaded(WidgetName.Workspaces))
            left.Append(WorkspaceWidget.New(services.Hyprland));

        if (IsLoaded(WidgetName.SystemStatus))
        {
            systemStatusButton = SystemStatusBarButton.New(services.SystemStatus);
            left.Append(systemStatusButton);
        }

        layout.SetStartWidget(left);
    }

    private void CreateCenter(CenterBox layout)
    {
        var center = Gtk.Box.New(Orientation.Horizontal, 5);

        if (IsLoaded(WidgetName.Media))
        {
            mediaButton = MediaBarButton.New(services.Media);
            center.Append(mediaButton);
        }

        if (IsLoaded(WidgetName.Calendar))
        {
            calendarButton = CalendarBarButton.New(services.Calendar);
            center.Append(calendarButton);
        }

        // Next to the date - the note is the note of this day
        if (IsLoaded(WidgetName.Obsidian))
        {
            obsidianButton = ObsidianBarButton.New(services.Obsidian);
            center.Append(obsidianButton);
        }

        layout.SetCenterWidget(center);
    }

    private void CreateEnd(CenterBox layout)
    {
        var end = Gtk.Box.New(Orientation.Horizontal, 5);

        if (IsLoaded(WidgetName.Tray))
        {
            trayButton = TrayBarButton.New(services.Tray);
            end.Append(trayButton);
        }

        if (IsLoaded(WidgetName.KdeConnect))
        {
            kdeConnectButton = KdeConnectBarButton.New(services.KdeConnect);
            end.Append(kdeConnectButton);
        }

        if (IsLoaded(WidgetName.Bluetooth))
        {
            bluetoothButton = BluetoothBarButton.New(services.Bluetooth);
            end.Append(bluetoothButton);
        }

        if (IsLoaded(WidgetName.Audio))
        {
            audioButton = AudioBarButton.New(services.Audio);
            end.Append(audioButton);
        }

        if (IsLoaded(WidgetName.Network))
        {
            networkButton = NetworkBarButton.New(services.Network);
            end.Append(networkButton);
        }

        if (IsLoaded(WidgetName.Clipboard))
        {
            clipboardButton = ClipboardBarButton.New(services.Clipboard);
            end.Append(clipboardButton);
        }

        if (IsLoaded(WidgetName.Trash))
        {
            trashButton = TrashBarButton.New(services.Trash);
            end.Append(trashButton);
        }

        if (IsLoaded(WidgetName.ThemeChanger))
            end.Append(ThemeChangerWidget.New());

        // Last, at the edge of the screen - where the notification popups will show
        if (IsLoaded(WidgetName.Notifications))
        {
            notificationsButton = NotificationsBarButton.New(services.Notifications);
            end.Append(notificationsButton);
        }

        layout.SetEndWidget(end);
    }
}
