using OvraelShell.Enums.Network;
using OvraelShell.Models.Network;

/// <summary>
/// Follows the last connect attempt of a device and reports it on its network
/// (<see cref="WirelessNetwork.ConnectStatus"/>, <see cref="WirelessNetwork.PasswordRejected"/>).
/// </summary>
public sealed class ConnectProgress
{
    private readonly NetworkDevice device;

    // Network of the last attempt, until it is activated or fails
    private WirelessNetwork? network;

    public ConnectProgress(NetworkDevice device)
    {
        this.device = device;
        device.State.Changed += OnStateChanged;
    }

    /// <summary>Shows the result of a Connect call on <paramref name="target"/>.</summary>
    public void Report(WirelessNetwork target, ConnectResult result)
    {
        switch (result)
        {
            case ConnectResult.AlreadyConnected:
                target.ConnectStatus.Set("Already connected to this network");
                break;

            case ConnectResult.Started:
                // A new attempt replaces the one still in progress
                if (network is not null && network != target)
                    network.ConnectStatus.Set(null);

                network = target;
                target.PasswordRejected.Set(false);
                target.ConnectStatus.Set("Connecting...");
                break;
        }
    }

    /// <summary>Moves the status of <paramref name="from"/>, and its attempt in progress, to the network replacing it.</summary>
    public void Transfer(WirelessNetwork from, WirelessNetwork to)
    {
        to.ConnectStatus.Set(from.ConnectStatus.Value);
        to.PasswordRejected.Set(from.PasswordRejected.Value);

        if (network == from)
            network = to;
    }

    private void OnStateChanged(DeviceState state)
    {
        if (network is null)
            return;

        switch (state)
        {
            case DeviceState.Activated:
                network.PasswordRejected.Set(false);
                network.ConnectStatus.Set(null);
                network = null;
                break;

            // Not handled: NeedAuth is passed on every activation, while NM fetches the
            // stored password. After a rejected one it asks secret agents for a new one -
            // either an agent provides it, or the attempt fails with NoSecrets.

            case DeviceState.Failed:
                var wrongPassword = device.StateReason.Value is DeviceStateReason.NoSecrets;

                network.PasswordRejected.Set(wrongPassword);
                network.ConnectStatus.Set(
                    wrongPassword ? "Wrong password" : device.StateReason.Value.ToFailureText()
                );
                network = null;
                break;
        }
    }
}
