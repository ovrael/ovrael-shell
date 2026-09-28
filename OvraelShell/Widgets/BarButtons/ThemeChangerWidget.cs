using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.Enums.Common;
using OvraelShell.InterfaceElements;
using OvraelShell.Utils.Theme;

namespace OvraelShell.Widgets;

[GObject.Subclass<Gtk.Box>]
public sealed partial class ThemeChangerWidget
{
    private Button button;

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

        schemeIndex = schemes.IndexOf(ThemeManager.UserTheme);

        button = Button.NewWithLabel(GetCurrentIcon());
        button.SetCursor(Cursors.Pointer);
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
        ThemeManager.ChangeTheme(schemes[schemeIndex]);
    }

    private void UpdateIcon()
    {
        button.Label = GetCurrentIcon();
    }

    private string GetCurrentIcon()
    {
        return ThemeManager.UserTheme switch
        {
            ThemeScheme.Dark => Icons.Theme.Dark,
            ThemeScheme.Light => Icons.Theme.Light,
            ThemeScheme.Auto => Icons.Theme.Auto,
            _ => Icons.Theme.Auto,
        };
    }
}
