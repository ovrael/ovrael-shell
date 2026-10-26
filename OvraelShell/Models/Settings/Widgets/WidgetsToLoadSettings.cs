using OvraelShell.Utils;

namespace OvraelShell.Models.Settings.Widgets;

/// <summary>
/// The widgets of the bar to load - true: load it, false: do not load or show it.
/// The service of a widget that is not loaded is not started.
/// </summary>
public sealed class WidgetsToLoadSettings
{
    private static readonly Dictionary<string, bool?> defaults = CreateDefaults();

    /// <summary>The names of the widgets to load, in the order of <see cref="CreateDefaults"/>.</summary>
    public ReactiveProperty<IReadOnlyList<string>> Loaded { get; } =
        new(ToLoaded(defaults), SequenceComparer<string>.Instance);

    public bool IsLoaded(string name) => Loaded.Value.Contains(name);

    /// <summary>All the widgets, in the order of the bar. The widgets that are not done yet are not loaded.</summary>
    internal static Dictionary<string, bool?> CreateDefaults() =>
        new()
        {
            [WidgetName.Weather] = true,
            [WidgetName.Caffeine] = true,
            [WidgetName.Workspaces] = true,
            [WidgetName.SystemStatus] = false,
            [WidgetName.Media] = false,
            [WidgetName.Calendar] = true,
            [WidgetName.Obsidian] = false,
            [WidgetName.Tray] = false,
            [WidgetName.KdeConnect] = false,
            [WidgetName.Bluetooth] = true,
            [WidgetName.Audio] = true,
            [WidgetName.Network] = true,
            [WidgetName.Clipboard] = false,
            [WidgetName.Trash] = false,
            [WidgetName.ThemeChanger] = true,
            [WidgetName.Notifications] = false,
        };

    /// <summary>Takes the values from the file. A missing or null value uses the default, an unknown name is logged.</summary>
    internal void Apply(Dictionary<string, bool?> widgets) => Loaded.Set(ToLoaded(widgets));

    private static string[] ToLoaded(Dictionary<string, bool?> widgets)
    {
        // Dictionary keys are not matched like the property names - so ignore the case here
        Dictionary<string, bool> values = [];
        foreach (var (name, value) in widgets)
        {
            var knownName = defaults.Keys.FirstOrDefault(known =>
                string.Equals(known, name, StringComparison.OrdinalIgnoreCase)
            );

            if (knownName is null)
                Console.WriteLine($"Settings: unknown widget \"{name}\" in widgetsToLoad, ignored.");
            else if (value is not null)
                values[knownName] = value.Value;
        }

        return defaults
            .Where(widget => values.GetValueOrDefault(widget.Key, widget.Value ?? false))
            .Select(widget => widget.Key)
            .ToArray();
    }
}
