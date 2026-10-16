using System.Net.Sockets;
using System.Text;
using OvraelShell.Models.Hyprland;

namespace OvraelShell.Hyprland;

/// <summary>A connection to the event socket. Create it with <see cref="HyprlandClient.ConnectEventsAsync"/>.</summary>
public sealed class HyprlandEventReader : IDisposable
{
    private const string Separator = ">>";

    private readonly StreamReader reader;

    internal HyprlandEventReader(Socket socket)
    {
        reader = new StreamReader(new NetworkStream(socket, ownsSocket: true), Encoding.UTF8);
    }

    /// <summary>The next event. Null when Hyprland closes the socket.</summary>
    public async Task<HyprlandEvent?> ReadAsync(CancellationToken token = default)
    {
        while (await reader.ReadLineAsync(token) is { } line)
        {
            var separatorIndex = line.IndexOf(Separator, StringComparison.Ordinal);
            if (separatorIndex < 0)
                continue;

            return new HyprlandEvent(
                line[..separatorIndex],
                line[(separatorIndex + Separator.Length)..]
            );
        }

        return null;
    }

    public void Dispose() => reader.Dispose();
}
