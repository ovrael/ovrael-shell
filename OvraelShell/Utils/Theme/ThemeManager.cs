using OvraelShell.Enums.Common;

namespace OvraelShell.Utils.Theme;

public static class ThemeManager
{
    public static ThemeScheme UserTheme { get; private set; } = ThemeScheme.Dark;
    public static event Action? ThemeChanged;

    private static ThemeScheme theme = ThemeScheme.Dark;
    private static string ColorScheme => theme == ThemeScheme.Light ? "Light" : "Dark";
    private static readonly CssLoader cssLoader = new();
    private static Gtk.Settings? gtkSettings;

    static ThemeManager()
    {
        gtkSettings = Gtk.Settings.GetDefault();

        gtkSettings?.OnNotify += (_, args) =>
        {
            if (
                args.Pspec.GetName() == "gtk-application-prefer-dark-theme"
                && UserTheme == ThemeScheme.Auto
            )
            {
                var isDark = gtkSettings.GtkApplicationPreferDarkTheme;
                ChangeTheme(isDark ? ThemeScheme.Dark : ThemeScheme.Light);
            }
        };
    }

    public static void Load(ThemeScheme loadTheme)
    {
        UserTheme = loadTheme;
        theme = loadTheme;

        cssLoader.LoadCss(ColorScheme);
    }

    public static void ChangeTheme(ThemeScheme newUserTheme)
    {
        if (!UpdateTheme(newUserTheme))
            return; // Nothing changed;

        cssLoader.SwitchTheme(ColorScheme);
        ThemeChanged?.Invoke();
    }

    private static bool UpdateTheme(ThemeScheme newTheme)
    {
        // Nothing changed
        if (UserTheme == newTheme)
            return false;

        UserTheme = newTheme;
        if (newTheme == ThemeScheme.Auto)
        {
            var autoTheme = IsAutoDarkMode() ? ThemeScheme.Dark : ThemeScheme.Light;
            if (autoTheme == theme) // Nothing changed for user, only change selection
                return false;
            theme = autoTheme;
        }
        else
        {
            theme = newTheme;
        }

        return true;
    }

    private static bool IsAutoDarkMode()
    {
        var settings = Gtk.Settings.GetDefault();
        return settings?.GtkApplicationPreferDarkTheme ?? false;
    }
}
