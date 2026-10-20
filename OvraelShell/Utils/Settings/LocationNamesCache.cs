using System.Text.Json;
using OvraelShell.Models.Settings.Weather;

namespace OvraelShell.Utils.Settings;

/// <summary>
/// The names from the reverse geocoding, in location-names.json next to the settings file -
/// so they are not looked up again after a restart, and the user can change them.
/// The file is watched - a change by the user sends <see cref="Changed"/>.
/// </summary>
public sealed class LocationNamesCache : IDisposable
{
    // Saving a file can send more than one event - read the file once, after the last one
    private const uint ReloadDelayMs = 100;

    public string FilePath { get; } =
        Path.Combine(Path.GetDirectoryName(UserSettings.FilePath)!, "location-names.json");

    // In the order of the file, so a save keeps the order of the user
    private List<LocationNameEntry> entries = [];

    // The text of the last save - the event of our own save does not read the file again
    private string? lastSaved;

    // An incorrect file is not saved over - the user can be in the middle of an edit
    private bool canSave = true;

    private readonly Gio.FileMonitor monitor;
    private uint reloadSourceId;

    /// <summary>The user changed the file.</summary>
    public event Action? Changed;

    /// <summary>Reads the file and starts to watch it. Create on the main thread.</summary>
    public LocationNamesCache()
    {
        Reload();

        monitor = Gio.FileHelper.NewForPath(FilePath).MonitorFile(Gio.FileMonitorFlags.None, null);
        monitor.OnChanged += OnFileChanged;
    }

    public bool TryGet(double latitude, double longitude, string language, out string name)
    {
        name = Find(latitude, longitude, language)?.Name ?? string.Empty;
        return name.Length > 0;
    }

    /// <summary>Adds or replaces the name and saves the file.</summary>
    public void Set(double latitude, double longitude, string language, string name)
    {
        if (Find(latitude, longitude, language) is { } entry)
        {
            if (entry.Name == name)
                return;

            entry.Name = name;
        }
        else
        {
            entries.Add(
                new()
                {
                    Name = name,
                    Language = language,
                    Latitude = latitude,
                    Longitude = longitude,
                }
            );
        }

        Save();
    }

    // The coordinates are the same doubles as in the settings - JSON keeps them exactly
    private LocationNameEntry? Find(double latitude, double longitude, string language) =>
        entries.FirstOrDefault(e =>
            e.Latitude == latitude && e.Longitude == longitude && e.Language == language
        );

    private void OnFileChanged(Gio.FileMonitor sender, Gio.FileMonitor.ChangedSignalArgs args)
    {
        // ChangesDoneHint - the file is written and closed.
        // Created - an editor that saves to a temporary file and moves it over this one.
        // Deleted - all the names are looked up again.
        if (
            args.EventType
            is not (
                Gio.FileMonitorEvent.ChangesDoneHint
                or Gio.FileMonitorEvent.Created
                or Gio.FileMonitorEvent.Deleted
            )
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
                if (Reload())
                    Changed?.Invoke();
                return false;
            }
        );
    }

    /// <summary>Reads the file. False when nothing changed - our own save, or an incorrect file.</summary>
    private bool Reload()
    {
        // No file - no names yet, or the user deleted it
        if (!File.Exists(FilePath))
        {
            lastSaved = null;
            canSave = true;
            entries = [];
            return true;
        }

        string text;
        LocationNamesFile? file;
        try
        {
            text = File.ReadAllText(FilePath);
            if (text == lastSaved)
                return false;

            file = JsonSerializer.Deserialize<LocationNamesFile>(text, UserSettings.jsonOptions);
        }
        catch (Exception exception)
            when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            Console.WriteLine(
                $"Weather: cannot read {FilePath}, it is not saved until it is correct: {exception.Message}"
            );
            canSave = false;
            return false;
        }

        canSave = true;

        // An entry without coordinates or language cannot be found - it is dropped on the next save
        entries = (file?.Locations ?? [])
            .OfType<LocationNameEntry>()
            .Where(e => e.Latitude is not null && e.Longitude is not null && e.Language is not null)
            .ToList();
        return true;
    }

    private void Save()
    {
        if (!canSave)
            return;

        try
        {
            var text = JsonSerializer.Serialize(
                new LocationNamesFile { Locations = [.. entries] },
                UserSettings.jsonOptions
            );

            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, text);
            lastSaved = text;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine($"Weather: cannot save {FilePath}: {exception.Message}");
        }
    }

    public void Dispose()
    {
        monitor.OnChanged -= OnFileChanged;
        monitor.Cancel();
        monitor.Dispose();

        if (reloadSourceId != 0)
        {
            GLib.Functions.SourceRemove(reloadSourceId);
            reloadSourceId = 0;
        }
    }
}
