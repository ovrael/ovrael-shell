using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;

namespace OvraelShell.Widgets.Bluetooth.Elements;

/// <summary>Name field with its own Save button, shown under a device row.</summary>
[GObject.Subclass<Gtk.Revealer>]
public sealed partial class NameRevealer
{
    private Entry entry;

    /// <summary>Raised with the typed name - empty means the name the device sends.</summary>
    public event Action<string>? Submitted;

    /// <summary>Raised on Escape - the field should close without a change.</summary>
    public event Action? Cancelled;

    public static new NameRevealer New()
    {
        return NewWithProperties([]);
    }

    [MemberNotNull(nameof(entry))]
    partial void Initialize()
    {
        entry = Entry.New();
        entry.Hexpand = true;
        entry.OnActivate += (_, _) => Submitted?.Invoke(entry.GetText());

        // Before the popover - it closes on Escape
        var keys = EventControllerKey.New();
        keys.OnKeyPressed += (_, args) =>
        {
            if (args.Keyval != Gdk.Constants.KEY_Escape)
                return false;

            Cancelled?.Invoke();
            return true;
        };
        entry.AddController(keys);

        var saveButton = Button.NewWithLabel("Save");
        saveButton.SetCursor(Cursors.Pointer);
        saveButton.OnClicked += (_, _) => Submitted?.Invoke(entry.GetText());

        var row = Box.New(Orientation.Horizontal, 10);
        row.Append(entry);
        row.Append(saveButton);

        SetChild(row);
    }

    /// <summary>
    /// Opens the field with <paramref name="name"/> selected, so typing replaces it.
    /// <paramref name="remoteName"/> shows when the field is empty.
    /// </summary>
    public void Open(string name, string remoteName)
    {
        entry.SetText(name);
        entry.SetPlaceholderText(remoteName);
        entry.SetTooltipText(
            string.IsNullOrEmpty(remoteName)
                ? null
                : $"Leave empty to use the name of the device: {remoteName}"
        );

        SetRevealChild(true);
        entry.GrabFocus();
        entry.SelectRegion(0, -1);
    }

    public void Close() => SetRevealChild(false);
}
