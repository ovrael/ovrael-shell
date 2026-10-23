using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using Gtk;
using OvraelShell.Hyprland;
using OvraelShell.InterfaceElements;

namespace OvraelShell.Widgets.Simple;

/// <summary>
/// The "stay awake" mode of Hyprland - the global Lua function caffeine(mode) of the Hyprland
/// config blocks idle, so hypridle does not dim, lock or suspend. Shows the mode and toggles it
/// on click. Hidden when Hyprland has no such function.
/// </summary>
[GObject.Subclass<Gtk.Box>]
public sealed partial class CaffeineWidget
{
    // The function starts caffeine.sh, which writes the PID file right after it starts
    // systemd-inhibit - give it time to finish
    private const uint RefreshDelayMs = 200;

    // Hyprland has no event for the mode. The file of caffeine.sh is followed instead,
    // so a keybind that changes the mode changes the widget too. Only a sign of a change -
    // the mode itself comes from the function.
    private static readonly string PidFilePath = Path.Combine(
        GetRuntimeDirectory(),
        "caffeine.pid"
    );

    // Null without Hyprland
    private static readonly HyprlandClient? Client = CreateClient();

    // Checked once for all the bars, so the message is written once
    private static Task<bool>? availability;

    private Button button;

    // Only while shown, so it does not keep the widget alive
    private Gio.FileMonitor? monitor;
    private uint refreshSourceId;

    // A reply of an earlier request can come after a later one - only the last one is shown
    private int refreshVersion;

    public static CaffeineWidget New()
    {
        return NewWithProperties([]);
    }

    [MemberNotNull(nameof(button))]
    partial void Initialize()
    {
        SetOrientation(Orientation.Horizontal);

        button = Button.NewWithLabel(Icons.Caffeine.Sleep);
        button.SetCursor(Cursors.Pointer);
        button.OnClicked += async (_, _) => await ToggleAsync();
        Append(button);

        OnMap += (_, _) =>
        {
            // The file exists only while the mode is on - GIO watches its folder for it
            monitor = Gio
                .FileHelper.NewForPath(PidFilePath)
                .MonitorFile(Gio.FileMonitorFlags.None, null);
            monitor.OnChanged += OnPidFileChanged;
            _ = RefreshAsync();
        };
        OnUnmap += (_, _) =>
        {
            if (monitor is not null)
            {
                monitor.OnChanged -= OnPidFileChanged;
                monitor.Cancel();
                monitor.Dispose();
                monitor = null;
            }

            if (refreshSourceId != 0)
            {
                GLib.Functions.SourceRemove(refreshSourceId);
                refreshSourceId = 0;
            }
        };

        // Shown (and so mapped) only when Hyprland has the function
        SetVisible(false);
        _ = ShowIfAvailableAsync();
    }

    private async Task ShowIfAvailableAsync()
    {
        if (await (availability ??= CheckAvailabilityAsync()))
            SetVisible(true);
    }

    private static async Task<bool> CheckAvailabilityAsync()
    {
        if (await RequestAsync("repl type(caffeine)") == "function")
            return true;

        Console.WriteLine(
            "Caffeine: Hyprland has no global Lua function caffeine(mode) - the widget is hidden."
        );
        return false;
    }

    // Created, changed or deleted - the mode is read again in each case
    private void OnPidFileChanged(Gio.FileMonitor sender, Gio.FileMonitor.ChangedSignalArgs args)
    {
        if (refreshSourceId != 0)
            GLib.Functions.SourceRemove(refreshSourceId);

        refreshSourceId = GLib.Functions.TimeoutAdd(
            GLib.Constants.PRIORITY_DEFAULT,
            RefreshDelayMs,
            () =>
            {
                refreshSourceId = 0;
                _ = RefreshAsync();
                return false;
            }
        );
    }

    // The application runs with the GLib synchronization context,
    // so the code after await runs on the main thread again
    private async Task RefreshAsync()
    {
        var version = ++refreshVersion;

        // repl returns the value - "on" or "off"
        var status = await RequestAsync("repl caffeine(\"status\")");
        if (version != refreshVersion || status is null)
            return;

        var isOn = status == "on";
        button.SetLabel(isOn ? Icons.Caffeine.Awake : Icons.Caffeine.Sleep);
        button.SetTooltipText(
            isOn
                ? "Stay awake: on - the screen does not dim, lock or sleep"
                : "Stay awake: off - the screen sleeps when idle"
        );
    }

    // The function only starts the script - the PID file tells when it is done
    private static async Task ToggleAsync()
    {
        // eval returns "ok", or the error of the Lua code
        var reply = await RequestAsync("eval caffeine(\"toggle\")");
        if (reply is not null && reply != "ok")
            Console.WriteLine($"Caffeine: cannot toggle the mode: {reply}");
    }

    /// <summary>The reply of Hyprland, or null when it cannot be asked.</summary>
    private static async Task<string?> RequestAsync(string command)
    {
        if (Client is null)
            return null;

        try
        {
            return (await Client.RequestAsync(command)).Trim();
        }
        catch (Exception exception) when (exception is SocketException or IOException)
        {
            Console.WriteLine($"Caffeine: cannot ask Hyprland: {exception.Message}");
            return null;
        }
    }

    private static HyprlandClient? CreateClient()
    {
        try
        {
            return new HyprlandClient();
        }
        catch (InvalidOperationException exception)
        {
            Console.WriteLine($"Caffeine: {exception.Message}");
            return null;
        }
    }

    private static string GetRuntimeDirectory()
    {
        var runtimeDirectory = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
        return string.IsNullOrEmpty(runtimeDirectory) ? "/tmp" : runtimeDirectory;
    }
}
