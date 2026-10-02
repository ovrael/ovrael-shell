using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;
using OvraelShell.Models.Network;

namespace OvraelShell.Widgets.Network.Elements;

[GObject.Subclass<Gtk.Box>]
public sealed partial class NetworkWirelessListItem
{
    WirelessNetwork network;
    Label strengthLabel;
    Label nameLabel;
    Label frequencyLabel;
    Label savedLabel;
    Label lockedLabel;
    PasswordRevealer passwordRevealer;
    Label statusLabel;
    ConnectButton connectButton;

    bool passwordRequested;
    bool wasPasswordRejected;

    /// <summary>Raised by the Connect / Join buttons. Password is null until the user is asked for it.</summary>
    public event Action<NetworkWirelessListItem, WirelessNetwork, string?>? ConnectRequested;

    [MemberNotNull(nameof(network))]
    public static NetworkWirelessListItem New()
    {
        return NewWithProperties([]);
    }

    partial void Initialize()
    {
        SetOrientation(Orientation.Vertical);
        SetMarginTop(12);
        SetMarginBottom(12);
        SetMarginStart(12);
        SetMarginEnd(12);
        SetSpacing(6);

        AppendContent();
        AddCssClass("network-wireless-listitem");
    }

    [MemberNotNull(
        nameof(strengthLabel),
        nameof(nameLabel),
        nameof(frequencyLabel),
        nameof(savedLabel),
        nameof(lockedLabel),
        nameof(passwordRevealer),
        nameof(statusLabel),
        nameof(connectButton)
    )]
    private void AppendContent()
    {
        strengthLabel = Label.New(string.Empty);
        frequencyLabel = Label.New(string.Empty);
        savedLabel = Label.New(string.Empty);
        lockedLabel = Label.New(string.Empty);

        nameLabel = Label.New("NETWORK_NAME");
        nameLabel.SetWidthChars(40);
        nameLabel.SetMaxWidthChars(40);
        nameLabel.SetXalign(0);
        nameLabel.Hexpand = true;

        connectButton = ConnectButton.New();
        connectButton.OnClicked += (_, _) => OnConnectClicked();

        var row = Box.New(Orientation.Horizontal, 10);
        row.Append(strengthLabel);
        row.Append(nameLabel);
        row.Append(frequencyLabel);
        row.Append(savedLabel);
        row.Append(lockedLabel);
        row.Append(connectButton);

        passwordRevealer = PasswordRevealer.New();
        passwordRevealer.Submitted += OnPasswordSubmitted;

        statusLabel = Label.New(string.Empty);
        statusLabel.SetXalign(0);
        statusLabel.SetWrap(true);

        // A long error wraps instead of widening the popover
        statusLabel.SetMaxWidthChars(40);
        statusLabel.Visible = false;

        Append(row);
        Append(passwordRevealer);
        Append(statusLabel);
    }

    /// <summary>Shows <paramref name="network"/> and follows its changes until <see cref="Unbind"/>.</summary>
    public void Bind(WirelessNetwork network)
    {
        // Rows are recycled - drop the password and status of the previous network
        if (this.network != network)
            ResetConnectState(network);

        this.network = network;
        network.OnChange += OnNetworkChanged;

        Refresh();
    }

    /// <summary>Stops following the network - otherwise it would update a recycled row.</summary>
    public void Unbind()
    {
        network?.OnChange -= OnNetworkChanged;
    }

    /// <summary>Shows the password field - called when the network has no saved profile.</summary>
    public void AskForPassword()
    {
        passwordRequested = true;
        UpdateConnectState();
    }

    public void ShowStatus(string? text)
    {
        statusLabel.SetLabel(text ?? string.Empty);
        statusLabel.Visible = !string.IsNullOrEmpty(text);
    }

    private void OnNetworkChanged(WirelessNetwork _) => Refresh();

    private void Refresh()
    {
        UpdateLabels();
        UpdateConnectState();
    }

    private void OnConnectClicked()
    {
        if (passwordRequested)
        {
            // Hide
            passwordRequested = false;
            UpdateConnectState();
        }
        else
        {
            ConnectRequested?.Invoke(this, network, null);
        }
    }

    private void OnPasswordSubmitted(string password)
    {
        passwordRequested = false;
        ConnectRequested?.Invoke(this, network, password);
    }

    private void ResetConnectState(WirelessNetwork network)
    {
        passwordRequested = network.PasswordRejected;
        wasPasswordRejected = network.PasswordRejected;
        passwordRevealer.Clear();
    }

    private void UpdateConnectState()
    {
        if (network.PasswordRejected && !wasPasswordRejected)
            passwordRequested = true;

        wasPasswordRejected = network.PasswordRejected;

        passwordRevealer.SetRevealed(passwordRequested);
        connectButton.ShowHideLabel(passwordRequested);
        ShowStatus(network.ConnectStatus);
    }

    private void UpdateLabels()
    {
        strengthLabel.SetLabel(NetworkIcons.Strength(network.Strength));
        strengthLabel.TooltipText = $"Strength: {network.Strength}";

        nameLabel.SetLabel(network.Ssid);
        nameLabel.TooltipMarkup =
            $"· SSID: {network.Ssid}\n· BSSID: {network.Bssid}\n· ObjectPath {network.ObjectPath}";

        frequencyLabel.SetLabel(NetworkIcons.Frequency(network.Frequency));
        frequencyLabel.TooltipText = $"Frequency: {network.Frequency}";

        savedLabel.SetLabel(NetworkIcons.Saved(network.IsSaved));
        savedLabel.TooltipText = $"Is saved? {network.IsSaved}";

        lockedLabel.SetLabel(NetworkIcons.Privacy(network.HasPrivacy));
        lockedLabel.TooltipText = network.HasPrivacy ? "Needs authentications" : "Open to connect";
    }
}
