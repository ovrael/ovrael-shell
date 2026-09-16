using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.ThemeManager;

namespace OvraelShell.Widgets;

[GObject.Subclass<Gtk.Box>]
public sealed partial class ThemeChangerWidget
{
    private Button button;
    private readonly Dictionary<string, string> icons = new()
    {
        { "dark", "" },
        { "light", "" },
        { "auto", "󰃡" },
    };

    private int schemeIndex = 1;
    private readonly ThemeScheme[] schemes =
    [
        ThemeScheme.Dark,
        ThemeScheme.Light,
        ThemeScheme.Auto,
    ];

    public static ThemeChangerWidget New()
    {
        return NewWithProperties([]);
    }

    [MemberNotNull(nameof(button))]
    partial void Initialize()
    {
        SetOrientation(Orientation.Horizontal);
        string icon = ThemeManager.ThemeManager.UserTheme switch
        {
            ThemeManager.ThemeScheme.Dark => icons["dark"],
            ThemeManager.ThemeScheme.Light => icons["light"],
            ThemeManager.ThemeScheme.Auto => icons["auto"],
            _ => icons["auto"],
        };
        schemeIndex = schemes.IndexOf(ThemeManager.ThemeManager.UserTheme);
        button = Button.NewWithLabel(icon);

        button.OnClicked += (_, _) =>
        {
            ChangeScheme();
            UpdateIcon();
        };

        Append(button);
    }

    private void ChangeScheme()
    {
        schemeIndex = (schemeIndex + 1) % schemes.Length;
        ThemeManager.ThemeManager.ChangeTheme(schemes[schemeIndex]);
    }

    private void UpdateIcon()
    {
        string icon = ThemeManager.ThemeManager.UserTheme switch
        {
            ThemeManager.ThemeScheme.Dark => icons["dark"],
            ThemeManager.ThemeScheme.Light => icons["light"],
            ThemeManager.ThemeScheme.Auto => icons["auto"],
            _ => icons["auto"],
        };
        button.Label = icon;
    }
}
