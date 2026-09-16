namespace OvraelShell.ThemeManager;

using Gtk;

public class CssLoader()
{
    private readonly Gdk.Display display =
        Gdk.Display.GetDefault() ?? throw new Exception("Cannot find default display!");
    private readonly string cssFolder = "Styles";
    private readonly string sharedFolder = "Shared";
    private readonly string colorsFolder = "ThemeColors";
    private readonly string themeFolder = "Themed";

    private readonly List<CssProvider> reloadableProviders = new();

    private string GetCssFolder(string subfolder) => $"{cssFolder}/{subfolder}";

    public void LoadCss(string colorScheme)
    {
        LoadCssFolder(GetCssFolder(sharedFolder), "*.gtk.css", false, null);
        LoadCssFolder(GetCssFolder(colorsFolder), $"{colorScheme}*.gtk.css", false, colorScheme);
        LoadCssFolder(GetCssFolder(themeFolder), "*.gtk.css", true, colorScheme);
    }

    public void SwitchTheme(string colorScheme)
    {
        UnloadProviders(display);
        LoadCssFolder(GetCssFolder(colorsFolder), $"{colorScheme}*.gtk.css", false, colorScheme);
        LoadCssFolder(GetCssFolder(themeFolder), "*.gtk.css", true, colorScheme);
    }

    private void LoadCssFolder(
        string folderPath,
        string searchPattern,
        bool isThemed,
        string? colorScheme
    )
    {
        string[] cssFiles = Directory.GetFiles(
            folderPath,
            searchPattern,
            SearchOption.AllDirectories
        );

        foreach (var cssPath in cssFiles)
        {
            Console.WriteLine($"Loading css file at {cssPath}");
            string cssText;
            uint priority;
            if (isThemed)
            {
                cssText = GetThemedCssText(cssPath, colorScheme);
                priority = Gtk.Constants.STYLE_PROVIDER_PRIORITY_USER;
            }
            else
            {
                cssText = File.ReadAllText(cssPath);
                priority = Gtk.Constants.STYLE_PROVIDER_PRIORITY_THEME;
            }

            var provider = CreateProvider(cssText, display, priority);
            if (colorScheme is not null)
                reloadableProviders.Add(provider);

            Console.WriteLine($"Provider for {cssPath} added with priority {priority}.");
        }
    }

    private CssProvider CreateProvider(string cssText, Gdk.Display display, uint priority)
    {
        var provider = CssProvider.New();
        provider.LoadFromString(cssText);
        StyleContext.AddProviderForDisplay(display, provider, priority);
        return provider;
    }

    private string GetThemedCssText(string filePath, string? colorScheme)
    {
        string[] cssTextLines = File.ReadAllLines(filePath);
        if (
            cssTextLines.Length > 0
            && cssTextLines[0].StartsWith("@import url")
            && colorScheme is not null
        )
        {
            cssTextLines[0] = $"@import url(\"{colorScheme}Colors.gtk.css\");";
        }
        return string.Concat(cssTextLines);
    }

    private void UnloadProviders(Gdk.Display display)
    {
        for (int i = reloadableProviders.Count - 1; i >= 0; i--)
        {
            var provider = reloadableProviders[i];
            Gtk.StyleContext.RemoveProviderForDisplay(display, provider);
            reloadableProviders.RemoveAt(i);
        }
    }
}
