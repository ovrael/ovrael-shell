using System.Net.Sockets;
using System.Text.Json;
using OvraelShell.Hyprland;
using OvraelShell.Models;
using OvraelShell.Models.Hyprland;

namespace OvraelShell.Services;

/// <summary>Follows Hyprland. The values change on the main thread.</summary>
public sealed class HyprlandService : IDisposable
{
    private readonly CancellationTokenSource cancellation = new();

    /// <summary>Id of the workspace on the focused monitor. Null when Hyprland is not available.</summary>
    public ReactiveProperty<int?> ActiveWorkspaceId { get; } = new();

    public HyprlandService()
    {
        HyprlandClient client;
        try
        {
            client = new HyprlandClient();
        }
        catch (InvalidOperationException exception)
        {
            Console.WriteLine($"Hyprland: {exception.Message} Workspaces are not followed.");
            return;
        }

        // The socket is read on a thread pool thread, so the main loop is not blocked
        _ = Task.Run(() => FollowEventsAsync(client, cancellation.Token));
    }

    private async Task FollowEventsAsync(HyprlandClient client, CancellationToken token)
    {
        try
        {
            // Connect before the request - a change between them is not lost
            using var events = await client.ConnectEventsAsync(token);

            var activeId = await client.GetActiveWorkspaceIdAsync(token);
            RunOnMainThread(() => ActiveWorkspaceId.Set(activeId), token);

            while (await events.ReadAsync(token) is { } hyprlandEvent)
            {
                if (GetActiveWorkspaceId(hyprlandEvent) is { } id)
                    RunOnMainThread(() => ActiveWorkspaceId.Set(id), token);
            }

            Console.WriteLine("Hyprland: the event socket was closed.");
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Disposed
        }
        catch (Exception exception)
            when (exception is SocketException or IOException or JsonException or KeyNotFoundException)
        {
            Console.WriteLine($"Hyprland: cannot follow the events: {exception.Message}");
        }
    }

    /// <summary>The new active workspace, if the event changes it.</summary>
    private static int? GetActiveWorkspaceId(HyprlandEvent hyprlandEvent)
    {
        var data = hyprlandEvent.Data;

        var idText = hyprlandEvent.Name switch
        {
            // workspacev2>>ID,NAME - other workspace on the focused monitor
            "workspacev2" => data[..Math.Max(data.IndexOf(','), 0)],

            // focusedmonv2>>MONITOR,ID - other monitor is focused. A monitor name has no comma.
            "focusedmonv2" => data[(data.LastIndexOf(',') + 1)..],

            _ => null,
        };

        return int.TryParse(idText, out var id) ? id : null;
    }

    private static void RunOnMainThread(Action action, CancellationToken token) =>
        GLib.Functions.IdleAdd(
            GLib.Constants.PRIORITY_DEFAULT,
            () =>
            {
                // Can come after Dispose - the widgets can be gone
                if (!token.IsCancellationRequested)
                    action();

                return false;
            }
        );

    // The token source is not disposed - the event task can still read the token
    public void Dispose() => cancellation.Cancel();
}
