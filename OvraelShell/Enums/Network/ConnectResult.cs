namespace OvraelShell.Enums.Network;

public enum ConnectResult
{
    /// <summary>NetworkManager accepted the request and started activating.</summary>
    Started,

    /// <summary>No saved profile and the network is secured - ask the user and call again.</summary>
    PasswordRequired,

    /// <summary>
    /// The network is the active access point of the device - nothing was sent to NM,
    /// re-activating it would only drop the connection and reconnect.
    /// </summary>
    AlreadyConnected,
}
