using System.Diagnostics;

namespace OvraelShell.Models.Network;

/// <summary>Download and upload speed in bytes per second.</summary>
public readonly record struct TrafficRate(double Download, double Upload)
{
    public static readonly TrafficRate Zero = new(0, 0);
}

/// <summary>
/// Measures the traffic of one network interface from the kernel counters in
/// /sys/class/net/{interface}/statistics - the same ones /proc/net/dev lists, but one file
/// per value, so there is no table to parse. Samples only between <see cref="Start"/> and <see cref="Stop"/>.
/// </summary>
public sealed class NetworkTraffic : IDisposable
{
    private const uint IntervalMs = 1000;

    private uint timeoutId;

    // Counters of the previous sample - null when they could not be read
    private (ulong Received, ulong Sent)? last;
    private long lastTimestamp;

    /// <summary>Null when there is nothing to measure (no active connection).</summary>
    public string? InterfaceName { get; private set; }

    /// <summary>Zero until the first interval has passed.</summary>
    public ReactiveProperty<TrafficRate> Rate { get; } = new(TrafficRate.Zero);

    public bool IsRunning => timeoutId != 0;

    /// <summary>Measures <paramref name="interfaceName"/> from now on - the rate starts again from zero.</summary>
    public void SetInterface(string? interfaceName)
    {
        if (interfaceName == InterfaceName)
            return;

        InterfaceName = interfaceName;
        Reset();
    }

    public void Start()
    {
        if (IsRunning)
            return;

        // Counters from before the start would spread the whole pause over one interval
        Reset();
        timeoutId = GLib.Functions.TimeoutAdd(0, IntervalMs, OnTick);
    }

    public void Stop()
    {
        if (!IsRunning)
            return;

        GLib.Functions.SourceRemove(timeoutId);
        timeoutId = 0;
    }

    private bool OnTick()
    {
        Sample();

        // Keep the timeout
        return true;
    }

    private void Reset()
    {
        last = ReadCounters();
        lastTimestamp = Stopwatch.GetTimestamp();
        Rate.Set(TrafficRate.Zero);
    }

    private void Sample()
    {
        var current = ReadCounters();
        var now = Stopwatch.GetTimestamp();
        var seconds = Stopwatch.GetElapsedTime(lastTimestamp, now).TotalSeconds;

        // Counters go back to zero when the interface is recreated
        var rate =
            current is { } counters && last is { } before
            && seconds > 0
            && counters.Received >= before.Received
            && counters.Sent >= before.Sent
                ? new TrafficRate(
                    (counters.Received - before.Received) / seconds,
                    (counters.Sent - before.Sent) / seconds
                )
                : TrafficRate.Zero;

        Rate.Set(rate);

        last = current;
        lastTimestamp = now;
    }

    private (ulong Received, ulong Sent)? ReadCounters()
    {
        if (InterfaceName is null)
            return null;

        var statistics = $"/sys/class/net/{InterfaceName}/statistics";

        try
        {
            return (
                ulong.Parse(File.ReadAllText($"{statistics}/rx_bytes")),
                ulong.Parse(File.ReadAllText($"{statistics}/tx_bytes"))
            );
        }
        catch (Exception ex)
            when (ex is IOException or UnauthorizedAccessException or FormatException)
        {
            // The interface disappeared in the meantime
            return null;
        }
    }

    public void Dispose() => Stop();
}
