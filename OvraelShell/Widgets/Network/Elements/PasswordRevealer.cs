using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;

namespace OvraelShell.Widgets.Network.Elements;

/// <summary>Password field with its own Connect button, shown under a network row.</summary>
[GObject.Subclass<Gtk.Revealer>]
public sealed partial class PasswordRevealer
{
    private PasswordEntry entry;

    /// <summary>Raised with a non-empty password; the field is cleared by then.</summary>
    public event Action<string>? Submitted;

    public static new PasswordRevealer New()
    {
        return NewWithProperties([]);
    }

    [MemberNotNull(nameof(entry))]
    partial void Initialize()
    {
        entry = PasswordEntry.New();
        entry.SetShowPeekIcon(true);
        entry.Hexpand = true;
        entry.OnActivate += (_, _) => Submit();

        var row = Box.New(Orientation.Horizontal, 10);
        row.Append(entry);
        row.Append(CreateSubmitButton());

        SetChild(row);
    }

    /// <summary>Opens or closes the field. The entry takes the focus when it opens.</summary>
    public void SetRevealed(bool revealed)
    {
        if (revealed && !GetRevealChild())
            entry.GrabFocus();

        SetRevealChild(revealed);
    }

    public void Clear() => entry.SetText(string.Empty);

    // The one that actually connects - enabled only once a password is typed
    private Button CreateSubmitButton()
    {
        var button = Button.NewWithLabel("Connect");
        button.SetCursor(Cursors.NotAllowed);
        button.Sensitive = false;
        button.OnClicked += (_, _) => Submit();

        entry.OnChanged += (_, _) =>
        {
            var hasPassword = !string.IsNullOrEmpty(entry.GetText());

            button.Sensitive = hasPassword;
            button.SetCursor(hasPassword ? Cursors.Pointer : Cursors.NotAllowed);
        };

        return button;
    }

    private void Submit()
    {
        var password = entry.GetText();

        if (string.IsNullOrEmpty(password))
            return;

        Clear();
        Submitted?.Invoke(password);
    }
}
