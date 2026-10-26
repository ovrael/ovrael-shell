using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.Services;
using OvraelShell.Widgets.Shared;

namespace OvraelShell.Widgets.Obsidian;

[GObject.Subclass<Gtk.Popover>]
public sealed partial class ObsidianPopover : IWithDisposableService<ObsidianService>
{
    public ObsidianService Service { get; private set; }

    private Box content;

    public static ObsidianPopover New(ObsidianService service, Button parent)
    {
        var widget = NewWithProperties([]);
        widget.AddService(service);
        widget.SetParent(parent);
        return widget;
    }

    [MemberNotNull(nameof(content))]
    partial void Initialize()
    {
        CreateContent();

        SetPosition(PositionType.Top);
        SetAutohide(true);
        SetChild(content);
        AddCssClass("popover");
    }

    [MemberNotNull(nameof(Service))]
    public void AddService(ObsidianService service)
    {
        Service = service;
    }

    // Safe to call more than once
    public void RemoveService() { }

    [MemberNotNull(nameof(content))]
    private void CreateContent()
    {
        content = PlaceholderPage.Create(
            "Daily note",
            "Vault from the settings (default: the open vault in ~/.config/obsidian/obsidian.json). Folder, date format and template from <vault>/.obsidian/daily-notes.json (no file: vault root, YYYY-MM-DD, no template). Open the note with obsidian://open?vault=...&file=... (Gio.AppInfo.LaunchDefaultForUri). Quick entry: append a line or a task (- [ ] ...) to today's note, create it from the template if missing. The vault is on an NFS hard mount: all file IO on Task.Run with a timeout, never on the main thread."
        );
    }
}
