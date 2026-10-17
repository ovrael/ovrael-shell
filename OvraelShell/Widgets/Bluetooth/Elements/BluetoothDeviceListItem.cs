using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;
using OvraelShell.Models.Bluetooth;

namespace OvraelShell.Widgets.Bluetooth.Elements;

/// <summary>
/// A device row: icon, name, state and battery, Rename, Forget and Connect / Disconnect.
/// Rename and Forget only for known (paired) devices.
/// </summary>
[GObject.Subclass<Gtk.Box>]
public sealed partial class BluetoothDeviceListItem
{
    private BluetoothDevice? device;

    private Label iconLabel;
    private Label nameLabel;
    private Label detailsLabel;
    private Spinner spinner;
    private Button renameButton;
    private Button forgetButton;
    private Button actionButton;
    private NameRevealer nameRevealer;
    private Label statusLabel;

    public static BluetoothDeviceListItem New()
    {
        return NewWithProperties([]);
    }

    [MemberNotNull(
        nameof(iconLabel),
        nameof(nameLabel),
        nameof(detailsLabel),
        nameof(spinner),
        nameof(renameButton),
        nameof(forgetButton),
        nameof(actionButton),
        nameof(nameRevealer),
        nameof(statusLabel)
    )]
    partial void Initialize()
    {
        SetOrientation(Orientation.Vertical);
        SetMarginTop(8);
        SetMarginBottom(8);
        SetMarginStart(12);
        SetMarginEnd(12);
        SetSpacing(6);

        iconLabel = Label.New(string.Empty);
        iconLabel.AddCssClass("bluetooth-listitem-icon");

        nameLabel = Label.New(string.Empty);
        nameLabel.SetXalign(0);
        nameLabel.SetWidthChars(30);
        nameLabel.SetMaxWidthChars(30);
        nameLabel.SetEllipsize(Pango.EllipsizeMode.End);

        detailsLabel = Label.New(string.Empty);
        detailsLabel.SetXalign(0);
        detailsLabel.AddCssClass("bluetooth-listitem-details");

        var texts = Box.New(Orientation.Vertical, 2);
        texts.Hexpand = true;
        texts.Append(nameLabel);
        texts.Append(detailsLabel);

        spinner = Spinner.New();

        renameButton = Button.NewWithLabel(Icons.Bluetooth.Rename);
        renameButton.SetTooltipText("Rename the device");
        renameButton.SetCursor(Cursors.Pointer);
        renameButton.OnClicked += (_, _) => ToggleRename();

        forgetButton = Button.NewWithLabel(Icons.Bluetooth.Forget);
        forgetButton.SetTooltipText("Forget the device");
        forgetButton.SetCursor(Cursors.Pointer);
        forgetButton.OnClicked += (_, _) => _ = device?.ForgetAsync();

        actionButton = Button.NewWithLabel("Connect");
        actionButton.SetCursor(Cursors.Pointer);
        actionButton.OnClicked += (_, _) => OnActionClicked();

        var row = Box.New(Orientation.Horizontal, 10);
        row.Append(iconLabel);
        row.Append(texts);
        row.Append(spinner);
        row.Append(renameButton);
        row.Append(forgetButton);
        row.Append(actionButton);

        statusLabel = Label.New(string.Empty);
        statusLabel.SetXalign(0);
        statusLabel.SetWrap(true);

        // A long error wraps instead of widening the popover
        statusLabel.SetMaxWidthChars(40);
        statusLabel.Visible = false;

        nameRevealer = NameRevealer.New();
        nameRevealer.Submitted += OnNameSubmitted;
        nameRevealer.Cancelled += () => nameRevealer.Close();

        Append(row);
        Append(nameRevealer);
        Append(statusLabel);
    }

    /// <summary>Shows <paramref name="device"/> and follows its changes until <see cref="Unbind"/>.</summary>
    public void Bind(BluetoothDevice device)
    {
        // Rows are recycled - the name field was for the previous device
        if (this.device != device)
            nameRevealer.Close();

        this.device = device;
        device.OnChange += OnDeviceChanged;

        Refresh(device);
    }

    /// <summary>Stops following the device - otherwise it would update a recycled row.</summary>
    public void Unbind()
    {
        device?.OnChange -= OnDeviceChanged;
        device = null;
    }

    private void OnDeviceChanged(BluetoothDevice changed) => Refresh(changed);

    private void OnActionClicked()
    {
        if (device is null)
            return;

        _ = device.Connected.Value ? device.DisconnectAsync() : device.ConnectAsync();
    }

    private void ToggleRename()
    {
        if (device is null)
            return;

        if (nameRevealer.GetRevealChild())
            nameRevealer.Close();
        else
            nameRevealer.Open(device.Name.Value, device.RemoteName.Value);
    }

    private void OnNameSubmitted(string name)
    {
        nameRevealer.Close();

        if (device is not null && name.Trim() != device.Name.Value)
            _ = device.RenameAsync(name);
    }

    private void Refresh(BluetoothDevice device)
    {
        iconLabel.SetLabel(BluetoothIcons.Device(device.Icon.Value));

        nameLabel.SetLabel(device.Name.Value);
        nameLabel.SetTooltipText(
            device.IsRenamed
                ? $"{device.Name.Value}\nDevice name: {device.RemoteName.Value}\n{device.Address}"
                : $"{device.Name.Value}\n{device.Address}"
        );

        detailsLabel.SetLabel(GetDetails(device));

        actionButton.SetLabel(
            device.Connected.Value ? "Disconnect"
            : device.Paired.Value ? "Connect"
            : "Pair"
        );

        var isBusy = device.IsBusy.Value;
        actionButton.Sensitive = !isBusy;
        renameButton.Sensitive = !isBusy;
        forgetButton.Sensitive = !isBusy;

        var isKnown = device.Paired.Value;
        renameButton.Visible = isKnown;
        forgetButton.Visible = isKnown;

        // Forgotten while the name field was open
        if (!isKnown)
            nameRevealer.Close();

        spinner.Visible = isBusy;
        spinner.Spinning = isBusy;

        statusLabel.SetLabel(device.Status.Value ?? string.Empty);
        statusLabel.Visible = device.Status.Value is not null;
    }

    private static string GetDetails(BluetoothDevice device)
    {
        var state =
            device.Connected.Value ? "Connected"
            : device.Paired.Value ? "Paired"
            : "Not paired";

        return device.Battery.Value is { } battery
            ? $"{state} · {BluetoothIcons.Battery(battery)} {battery}%"
            : state;
    }
}
