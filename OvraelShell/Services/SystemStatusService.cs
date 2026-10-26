namespace OvraelShell.Services;

/// <summary>Follows the load and the temperatures of the computer. The values change on the main thread.</summary>
/// <remarks>
/// TODO: not implemented yet. The plan:
/// - CPU load from /proc/stat (difference of two samples), RAM and swap from /proc/meminfo.
/// - CPU temperature from /sys/class/hwmon (coretemp), board sensors (nct6687).
/// - NVIDIA GPU load and temperature through NVML (libnvidia-ml.so), not by starting nvidia-smi.
/// - Disk usage with statvfs - never for network mounts on the main thread (NFS hard mounts block).
/// - Sample with TimeoutAdd: slowly for the bar label, faster only while the popover is open.
/// </remarks>
public sealed class SystemStatusService : IDisposable
{
    public void Dispose() { }
}
