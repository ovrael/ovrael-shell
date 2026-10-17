namespace OvraelShell.InterfaceElements;

/// <summary>Picks the icon for a Bluetooth value.</summary>
public static class BluetoothIcons
{
    public static string State(bool isPowered, bool isConnected)
    {
        if (!isPowered)
            return Icons.Bluetooth.Off;

        return isConnected ? Icons.Bluetooth.Connected : Icons.Bluetooth.On;
    }

    /// <summary>By the freedesktop icon name BlueZ gives the device, e.g. audio-headset.</summary>
    public static string Device(string iconName)
    {
        return iconName switch
        {
            "audio-headset" => Icons.Bluetooth.Headset,
            "audio-headphones" => Icons.Bluetooth.Headphones,
            "audio-card" or "audio-speakers" => Icons.Bluetooth.Speaker,
            "input-gaming" => Icons.Bluetooth.Gamepad,
            "input-keyboard" => Icons.Bluetooth.Keyboard,
            "input-mouse" => Icons.Bluetooth.Mouse,
            "input-tablet" => Icons.Bluetooth.Tablet,
            "phone" => Icons.Bluetooth.Phone,
            "computer" => Icons.Bluetooth.Computer,
            "video-display" => Icons.Bluetooth.Display,
            "printer" => Icons.Bluetooth.Printer,
            "camera-photo" or "camera-video" => Icons.Bluetooth.Camera,
            _ when iconName.Contains("watch") => Icons.Bluetooth.Watch,
            _ => Icons.Bluetooth.Device,
        };
    }

    public static string Battery(byte percentage)
    {
        var level = (Math.Min((int)percentage, 100) + 5) / 10;
        return Icons.Bluetooth.Battery[level];
    }
}
