using OvraelShell.Enums.Settings;
using OvraelShell.Utils.Settings;

namespace OvraelShell.Utils.Theme;

/// <summary>
/// The theme from the settings file. <see cref="ChangeTheme"/> (the theme button) changes it
/// until the theme in the file changes - a save with the same theme keeps the current one.
/// </summary>
public static class ThemeManager
{
    private static readonly CssLoader cssLoader = new();
    private static Gtk.Settings? gtkSettings;
    private static bool isLoaded;

    /// <summary>The chosen theme - <see cref="ThemeScheme.System"/> follows the desktop.</summary>
    public static ThemeScheme UserTheme { get; private set; } = ThemeScheme.Dark;

    /// <summary>Sent when <see cref="UserTheme"/> or the loaded colors change.</summary>
    public static event Action? ThemeChanged;

    // Dark or Light - the colors that are loaded
    private static ThemeScheme theme = ThemeScheme.Dark;
    private static string ColorScheme => theme == ThemeScheme.Light ? "Light" : "Dark";

    /// <summary>
    /// Loads the styles with the theme from the settings and follows it.
    /// Call after <see cref="UserSettings.Load"/>. Safe to call more than once.
    /// </summary>
    public static void Load()
    {
        if (isLoaded)
            return;

        isLoaded = true;

        // Both live as long as the application - no need to unsubscribe
        gtkSettings = Gtk.Settings.GetDefault();
        gtkSettings?.OnNotify += (_, args) =>
        {
            var name = args.Pspec.GetName();
            if (
                UserTheme == ThemeScheme.System
                && name is ("gtk-interface-color-scheme" or "gtk-application-prefer-dark-theme")
                && ApplyTheme()
            )
                ThemeChanged?.Invoke();
        };

        UserSettings.General.Theme.Changed += ChangeTheme;

        UserTheme = UserSettings.General.Theme.Value;
        theme = ToColors(UserTheme);
        cssLoader.LoadCss(ColorScheme);
    }

    public static void ChangeTheme(ThemeScheme newUserTheme)
    {
        if (UserTheme == newUserTheme)
            return;

        UserTheme = newUserTheme;
        ApplyTheme();
        ThemeChanged?.Invoke();
    }

    // Loads the colors of UserTheme. False when they are already loaded.
    private static bool ApplyTheme()
    {
        var newTheme = ToColors(UserTheme);
        if (newTheme == theme)
            return false;

        theme = newTheme;
        cssLoader.SwitchTheme(ColorScheme);
        return true;
    }

    private static ThemeScheme ToColors(ThemeScheme userTheme)
    {
        if (userTheme != ThemeScheme.System)
            return userTheme;

        return IsSystemDark() ? ThemeScheme.Dark : ThemeScheme.Light;
    }

    // The interface color scheme comes from the desktop (the settings portal).
    // gtk-application-prefer-dark-theme - from settings.ini, if the portal has no color scheme.
    private static bool IsSystemDark() =>
        gtkSettings?.GtkInterfaceColorScheme == Gtk.InterfaceColorScheme.Dark
        || (gtkSettings?.GtkApplicationPreferDarkTheme ?? false);
}
