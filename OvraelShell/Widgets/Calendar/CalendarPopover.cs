using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.Models;
using OvraelShell.Services;
using OvraelShell.Widgets.Shared;

namespace OvraelShell.Widgets.Calendar;

[GObject.Subclass<Gtk.Popover>]
public sealed partial class CalendarPopover : IWithDisposableService<CalendarService>
{
    public CalendarService Service { get; private set; }

    private readonly ReactiveProperty<int> monthDifference = new(0);
    private readonly ReactiveProperty<int> yearDifference = new(0);

    private Box content;

    public static CalendarPopover New(CalendarService service, Button parent)
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
    public void AddService(CalendarService service)
    {
        Service = service;
    }

    // Safe to call more than once
    public void RemoveService() { }

    [MemberNotNull(nameof(content))]
    private void CreateContent()
    {
        content = Box.New(Orientation.Vertical, 8);
        content.SetMarginTop(12);
        content.SetMarginBottom(12);
        content.SetMarginStart(12);
        content.SetMarginEnd(12);

        var month = CalendarMonth.New(
            DateOnly.FromDateTime(DateTime.Now),
            monthDifference.Value,
            yearDifference.Value
        );
        content.Append(month);
    }
}
