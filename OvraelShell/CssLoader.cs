using Gtk;

public class CssLoader(CssTheme theme = CssTheme.Dark)
{
    public CssTheme UserTheme { get; private set; } = theme;
    CssTheme theme = theme;
    private readonly Gdk.Display display =
        Gdk.Display.GetDefault() ?? throw new Exception("Cannot find default display!");
    private readonly string cssFolder = "Styles";
    private readonly string sharedFolder = "Shared";
    private readonly string darkThemeFolder = "DarkTheme";
    private readonly string lightThemeFolder = "LightTheme";

    private string GetCssFolder(string subfolder) => $"{cssFolder}/{subfolder}";

    public void LoadCss()
    {
        LoadCssFolder(GetCssFolder(sharedFolder));
        LoadTheme(theme);
    }

    public void SwitchTheme(CssTheme newUserTheme)
    {
        UserTheme = newUserTheme;
        if (newUserTheme == CssTheme.Auto)
        {
            theme = IsAutoDarkMode() ? CssTheme.Dark : CssTheme.Light;
        }
        else
        {
            theme = newUserTheme;
        }
    }

    private bool IsAutoDarkMode()
    {
        var settings = Gtk.Settings.GetDefault();
        return settings?.GtkApplicationPreferDarkTheme ?? false;
    }

    private void LoadTheme(CssTheme theme)
    {
        if (theme == CssTheme.Auto)
            throw new Exception("Cannot load AUTO theme, something went wrong!");

        string themeFolder = theme == CssTheme.Dark ? darkThemeFolder : lightThemeFolder;
        LoadCssFolder(GetCssFolder(themeFolder), Gtk.Constants.STYLE_PROVIDER_PRIORITY_USER);
    }

    private void LoadCssFolder(
        string folderPath,
        uint priority = Gtk.Constants.STYLE_PROVIDER_PRIORITY_THEME
    )
    {
        string[] cssFiles = Directory.GetFiles(folderPath, "*.css", SearchOption.AllDirectories);

        foreach (var cssPath in cssFiles)
        {
            AddProvider(cssPath, display, priority);
        }
    }

    private void AddProvider(
        string path,
        Gdk.Display display,
        uint priority = Gtk.Constants.STYLE_PROVIDER_PRIORITY_THEME
    )
    {
        Console.WriteLine($"Loading css file at {path}");
        var provider = CssProvider.New();
        provider.LoadFromPath(path);
        StyleContext.AddProviderForDisplay(display, provider, priority);
        Console.WriteLine($"Provider for {path} added with priority {priority}.");
        // int lastSlashIndex = path.LastIndexOf('/');
        // int underscoreIndex = path.LastIndexOf('_');

        // if (lastSlashIndex == -1 || underscoreIndex == -1)
        //     throw new Exception("Invalid css style file path!");

        // string filePriority = path[(lastSlashIndex + 1)..underscoreIndex];
        // if (uint.TryParse(filePriority, out uint priority))
        // {
        //     var provider = CssProvider.New();
        //     provider.LoadFromPath(path);
        //     StyleContext.AddProviderForDisplay(display, provider, priority);
        //     Console.WriteLine($"Provider for {path} added with priority {priority}.");
        // }
        // else
        // {
        //     Console.WriteLine($"Cannot parse `{filePriority}` into uint, provider is not added.");
        // }
    }
}
