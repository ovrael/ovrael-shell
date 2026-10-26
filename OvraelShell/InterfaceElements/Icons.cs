namespace OvraelShell.InterfaceElements;

public static class Icons
{
    public static class Common
    {
        public static readonly string SpeedSlow = "󰾆";
        public static readonly string SpeedMid = "󰾅";
        public static readonly string SpeedFast = "󰓅";
        public static readonly string Saved = "󰆓";
        public static readonly string Unsaved = "󱙃";
        public static readonly string Locked = "";
        public static readonly string Unlocked = "";
    }

    public static class Theme
    {
        public static readonly string Dark = "";
        public static readonly string Light = "";
        public static readonly string Auto = "";
    }

    public static class Network
    {
        public static readonly string WifiConnected = "";
        public static readonly string WifiDisconnected = "󰖪";
        public static readonly string EthernetConnected = "󰈁";
        public static readonly string EthernetDisconnected = "󰈂";
        public static readonly string SignalStrengthVeryLow = "󰢿";
        public static readonly string SignalStrengthLow = "󰢼";
        public static readonly string SignalStrengthMedium = "󰢽";
        public static readonly string SignalStrengthHigh = "󰢾";
        public static readonly string Download = "󰇚";
        public static readonly string Upload = "󰕒";
    }

    public static class Audio
    {
        public static readonly string VolumeOff = "";
        public static readonly string VolumeNone = "";
        public static readonly string VolumeLow = "";
        public static readonly string VolumeMedium = "";
        public static readonly string VolumeHigh = "";
        public static readonly string Output = "󰓃";
        public static readonly string Input = "󰍬";
        public static readonly string InputMuted = "󰍭";
        public static readonly string Stream = "󰝚";
        public static readonly string IsDefault = "󰓎";
        public static readonly string SetDefault = "󰓒";
    }

    public static class Bluetooth
    {
        public static readonly string On = "󰂯";
        public static readonly string Connected = "󰂱";
        public static readonly string Off = "󰂲";
        public static readonly string Forget = "󰆴";
        public static readonly string Rename = "󰏫";
        public static readonly string Device = "󰂯";
        public static readonly string Headset = "󰋎";
        public static readonly string Headphones = "󰋋";
        public static readonly string Speaker = "󰓃";
        public static readonly string Gamepad = "󰊗";
        public static readonly string Keyboard = "󰌌";
        public static readonly string Mouse = "󰍽";
        public static readonly string Phone = "󰄜";
        public static readonly string Computer = "󰌢";
        public static readonly string Display = "󰍹";
        public static readonly string Printer = "󰐪";
        public static readonly string Watch = "󰖉";
        public static readonly string Tablet = "󰓶";
        public static readonly string Camera = "󰄀";

        // 0%, 10%, ... 100%
        public static readonly string[] Battery =
        [
            "󰂎",
            "󰁺",
            "󰁻",
            "󰁼",
            "󰁽",
            "󰁾",
            "󰁿",
            "󰂀",
            "󰂁",
            "󰂂",
            "󰁹",
        ];
    }

    public static class Caffeine
    {
        public static readonly string Awake = "󰅶";
        public static readonly string Sleep = "󰒲";
    }

    public static class Workspace
    {
        public static readonly string Other = "?";
    }

    public static class Weather
    {
        public static readonly string Other = "?";

        // nf_weather_
        public static readonly string cloudyFull = "";
        public static readonly string precipitationProbability = "";

        // DAY
        public static readonly string daySun = "";
        public static readonly string dayCloudyMid = "";
        public static readonly string dayRain = "";
        public static readonly string daySnow = "";
        public static readonly string dayThunderstorm = "";

        // NIGHT
        public static readonly string nightMoon = "";
        public static readonly string nightCloudyMid = "";
        public static readonly string nightRain = "";
        public static readonly string nightSnow = "";
        public static readonly string nightThunderstorm = "";
    }

    public static class SystemStatus
    {
        public static readonly string Cpu = "󰘚";
        public static readonly string Memory = "󰍛";
        public static readonly string Temperature = "󰔏";
        public static readonly string Disk = "󰋊";
    }

    public static class Obsidian
    {
        public static readonly string Default = "󰠮";
        public static readonly string Edit = "󱓧";
    }

    public static class Clipboard
    {
        public static readonly string Default = "󰅍";
        public static readonly string Empty = "󰅌";
    }

    public static class Media
    {
        public static readonly string Default = "󰝚";
        public static readonly string Play = "󰐊";
        public static readonly string Pause = "󰏤";
        public static readonly string Next = "󰒭";
        public static readonly string Previous = "󰒮";
    }

    public static class Notifications
    {
        public static readonly string Default = "󰂜";
        public static readonly string Unread = "󱅫";
        public static readonly string DoNotDisturb = "󰂛";
    }

    public static class Tray
    {
        public static readonly string Default = "󰅃";
        public static readonly string Drive = "󱊞";
        public static readonly string Eject = "󰇪";
        public static readonly string Service = "󰒓";
    }

    public static class KdeConnect
    {
        public static readonly string Connected = "󰄡";
        public static readonly string Disconnected = "󰥐";
    }

    public static class Trash
    {
        public static readonly string Empty = "󰩺";
        public static readonly string Full = "󰩹";
        public static readonly string Restore = "󰠙";
        public static readonly string EmptyAll = "󰛌";
    }
}
