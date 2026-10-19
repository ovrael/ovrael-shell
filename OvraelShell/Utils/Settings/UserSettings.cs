using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using OvraelShell.Models.Settings;
using OvraelShell.Models.Settings.General;
using OvraelShell.Models.Settings.Hyprland;

namespace OvraelShell.Utils.Settings;

/// <summary>
/// User settings from $XDG_CONFIG_HOME/ovraelshell/settings.json. The file is watched -
/// when it is saved, the values are read again and the changed ones send Changed.
/// </summary>
public static class UserSettings
{
    // Saving a file can send more than one event - read the file once, after the last one
    private const uint ReloadDelayMs = 100;

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        IndentSize = 4,
        Encoder = MinimalJsonEncoder.Instance,
        Converters = { new JsonStringEnumConverter() },
    };

    private static Gio.FileMonitor? monitor;
    private static uint reloadSourceId;

    public static string FilePath { get; } =
        Path.Combine(GetConfigDirectory(), "ovraelshell", "settings.json");

    public static GeneralSettings General { get; } = new();

    public static HyprWorkspacesSettings HyprWorkspaces { get; } = new();

    /// <summary>
    /// Reads the file (creates it with the defaults if it does not exist) and starts to watch it.
    /// Call on the main thread - the file events come on it. Safe to call more than once.
    /// </summary>
    public static void Load()
    {
        if (monitor is not null)
            return;

        CreateDefaultFile();
        Reload();

        ApplyCulture(General.LanguageCode.Value);
        General.LanguageCode.Changed += ApplyCulture;

        monitor = Gio.FileHelper.NewForPath(FilePath).MonitorFile(Gio.FileMonitorFlags.None, null);
        monitor.OnChanged += OnFileChanged;
    }

    /// <summary>Stops watching the file. The current values stay.</summary>
    public static void Unload()
    {
        if (monitor is null)
            return;

        monitor.OnChanged -= OnFileChanged;
        monitor.Cancel();
        monitor.Dispose();
        monitor = null;

        General.LanguageCode.Changed -= ApplyCulture;

        if (reloadSourceId != 0)
        {
            GLib.Functions.SourceRemove(reloadSourceId);
            reloadSourceId = 0;
        }
    }

    private static void OnFileChanged(
        Gio.FileMonitor sender,
        Gio.FileMonitor.ChangedSignalArgs args
    )
    {
        // ChangesDoneHint - the file is written and closed.
        // Created - an editor that saves to a temporary file and moves it over this one.
        if (
            args.EventType
            is not (Gio.FileMonitorEvent.ChangesDoneHint or Gio.FileMonitorEvent.Created)
        )
            return;

        if (reloadSourceId != 0)
            GLib.Functions.SourceRemove(reloadSourceId);

        reloadSourceId = GLib.Functions.TimeoutAdd(
            GLib.Constants.PRIORITY_DEFAULT,
            ReloadDelayMs,
            () =>
            {
                reloadSourceId = 0;
                Reload();
                return false;
            }
        );
    }

    /// <summary>Reads the file. If it cannot be read or parsed, the current values stay.</summary>
    private static void Reload()
    {
        SettingsFile? file;
        try
        {
            using var stream = File.OpenRead(FilePath);
            file = JsonSerializer.Deserialize<SettingsFile>(stream, jsonOptions);
        }
        catch (Exception exception)
            when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            Console.WriteLine(
                $"Settings: cannot read {FilePath}, the current settings stay: {exception.Message}"
            );
            return;
        }

        // A literal null in the file - same as an empty object
        file ??= new SettingsFile { General = null, HyprWorkspaces = null };

        General.Apply(file.General ?? new GeneralSection());
        HyprWorkspaces.Apply(file.HyprWorkspaces ?? new HyprWorkspacesSection());

        Console.WriteLine($"Settings: loaded {FilePath}");
    }

    private static void CreateDefaultFile()
    {
        if (File.Exists(FilePath))
            return;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(new SettingsFile(), jsonOptions));

            Console.WriteLine($"Settings: created {FilePath} with the default settings");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine($"Settings: cannot create {FilePath}: {exception.Message}");
        }
    }

    // The language code is validated in GeneralSettings, so the culture exists
    private static void ApplyCulture(string languageCode)
    {
        var culture = CultureInfo.GetCultureInfo(languageCode);

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    private static string GetConfigDirectory()
    {
        var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (!string.IsNullOrEmpty(configHome))
            return configHome;

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config"
        );
    }
}
