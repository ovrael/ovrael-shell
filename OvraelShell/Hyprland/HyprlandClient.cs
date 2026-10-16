using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace OvraelShell.Hyprland;

public sealed class HyprlandClient
{
    private readonly string _runtimeDirectory;
    private readonly string _instanceSignature;

    private string CommandSocket =>
        Path.Combine(_runtimeDirectory, "hypr", _instanceSignature, ".socket.sock");

    private string EventSocket =>
        Path.Combine(_runtimeDirectory, "hypr", _instanceSignature, ".socket2.sock");

    public HyprlandClient()
    {
        _runtimeDirectory =
            Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR")
            ?? throw new InvalidOperationException("XDG_RUNTIME_DIR is not set.");

        _instanceSignature =
            Environment.GetEnvironmentVariable("HYPRLAND_INSTANCE_SIGNATURE")
            ?? throw new InvalidOperationException("HYPRLAND_INSTANCE_SIGNATURE is not set.");
    }

    /// <summary>Sends a command (as for hyprctl, "j/" in front gives JSON) and returns the reply.</summary>
    public async Task<string> RequestAsync(string command, CancellationToken token = default)
    {
        using var socket = await ConnectAsync(CommandSocket, token);
        await socket.SendAsync(Encoding.UTF8.GetBytes(command), SocketFlags.None, token);

        // Hyprland closes the connection after the reply
        using var reader = new StreamReader(new NetworkStream(socket), Encoding.UTF8);
        return await reader.ReadToEndAsync(token);
    }

    /// <summary>Id of the workspace on the focused monitor.</summary>
    public async Task<int> GetActiveWorkspaceIdAsync(CancellationToken token = default)
    {
        var reply = await RequestAsync("j/activeworkspace", token);

        using var json = JsonDocument.Parse(reply);
        return json.RootElement.GetProperty("id").GetInt32();
    }

    /// <summary>Connects to the event socket. Events that come after this call are not lost.</summary>
    public async Task<HyprlandEventReader> ConnectEventsAsync(CancellationToken token = default) =>
        new(await ConnectAsync(EventSocket, token));

    private static async Task<Socket> ConnectAsync(string path, CancellationToken token)
    {
        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        try
        {
            await socket.ConnectAsync(new UnixDomainSocketEndPoint(path), token);
            return socket;
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
