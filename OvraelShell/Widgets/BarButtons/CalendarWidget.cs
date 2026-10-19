using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.Enums.Settings;
using OvraelShell.Utils.Settings;

namespace OvraelShell.Widgets;

[GObject.Subclass<Gtk.Box>]
public sealed partial class CalendarWidget
{
    private uint timeoutId;
    private Label label;

    public static CalendarWidget New()
    {
        return NewWithProperties([]);
    }

    [MemberNotNull(nameof(label))]
    partial void Initialize()
    {
        SetOrientation(Orientation.Horizontal);
        label = Label.New(null);
        Update();
        timeoutId = GLib.Functions.TimeoutAdd(0, 1000, Update);
        Append(label);

        // Settings are static - follow them only while shown, so they do not keep the widget alive
        OnMap += (_, _) =>
        {
            UserSettings.General.TimeFormat.Changed += OnTimeFormatChanged;
            UserSettings.General.DateFormat.Changed += OnDateFormatChanged;
            Update();
        };
        OnUnmap += (_, _) =>
        {
            UserSettings.General.TimeFormat.Changed -= OnTimeFormatChanged;
            UserSettings.General.DateFormat.Changed -= OnDateFormatChanged;
        };

        OnDestroy += (_, _) => GLib.Functions.SourceRemove(timeoutId);
    }

    private void OnTimeFormatChanged(TimeFormat _) => Update();

    private void OnDateFormatChanged(string _) => Update();

    private bool Update()
    {
        var now = DateTime.Now;

        label.SetText(now.ToString(UserSettings.General.TimeFormatString));
        label.SetTooltipText(now.ToString(UserSettings.General.DateFormatString));
        return true;
    }
}
