using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.Enums.Settings;
using OvraelShell.InterfaceElements;
using OvraelShell.Utils.Theme;

namespace OvraelShell.Widgets.Simple;

/// <summary>
/// Shows the theme and changes it on click: dark, light, system. Only a shortcut - the theme
/// comes from settings.json, and a change of the theme there replaces the one chosen here.
/// </summary>
[GObject.Subclass<Gtk.Box>]
public sealed partial class ThemeChangerWidget
{
    private Button button;

    public static ThemeChangerWidget New()
    {
        return NewWithProperties([]);
    }

    [MemberNotNull(nameof(button))]
    partial void Initialize()
    {
        SetOrientation(Orientation.Horizontal);

        button = Button.New();
        button.SetCursor(Cursors.Pointer);
        button.OnClicked += (_, _) => ThemeManager.ChangeTheme(Next(ThemeManager.UserTheme));
        Append(button);

        // Only while shown - the theme can change in the file or on another bar
        OnMap += (_, _) =>
        {
            ThemeManager.ThemeChanged += Update;
            Update();
        };
        OnUnmap += (_, _) => ThemeManager.ThemeChanged -= Update;
    }

    private void Update()
    {
        var theme = ThemeManager.UserTheme;

        button.SetLabel(
            theme switch
            {
                ThemeScheme.Light => Icons.Theme.Light,
                ThemeScheme.System => Icons.Theme.System,
                _ => Icons.Theme.Dark,
            }
        );
        button.SetTooltipText($"Theme: {theme.ToString().ToLowerInvariant()}");
    }

    private static ThemeScheme Next(ThemeScheme theme) =>
        theme switch
        {
            ThemeScheme.Dark => ThemeScheme.Light,
            ThemeScheme.Light => ThemeScheme.System,
            _ => ThemeScheme.Dark,
        };
}
