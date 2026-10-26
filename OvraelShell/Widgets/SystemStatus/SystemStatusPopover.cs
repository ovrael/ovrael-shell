using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.Services;
using OvraelShell.Widgets.Shared;

namespace OvraelShell.Widgets.SystemStatus;

[GObject.Subclass<Gtk.Popover>]
public sealed partial class SystemStatusPopover : IWithDisposableService<SystemStatusService>
{
    public SystemStatusService Service { get; private set; }

    private Box content;

    public static SystemStatusPopover New(SystemStatusService service, Button parent)
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
    public void AddService(SystemStatusService service)
    {
        Service = service;
    }

    // Safe to call more than once
    public void RemoveService() { }

    [MemberNotNull(nameof(content))]
    private void CreateContent()
    {
        content = PlaceholderPage.Create(
            "System status",
            "CPU load from /proc/stat (difference of two samples), RAM and swap from /proc/meminfo. CPU temperature from /sys/class/hwmon (coretemp), board sensors (nct6687). NVIDIA GPU load and temperature through NVML (libnvidia-ml.so), not by starting nvidia-smi. Disk usage with statvfs - never for network mounts on the main thread (NFS hard mounts block). Sample with TimeoutAdd: slowly for the bar label, faster only while the popover is open."
        );
    }
}
