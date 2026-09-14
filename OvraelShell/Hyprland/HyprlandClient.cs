using System.Net.Sockets;
using System.Text;

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
}
