using System.Data;
using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;
using OvraelShell.Models;
using OvraelShell.Models.Audio.PipeWire;
using OvraelShell.Services;
using ZwlrLayerShell;

namespace OvraelShell.Windows;

[GObject.Subclass<Gtk.Window>]
public sealed partial class AudioVolumeLevelWindow
{
    Label? iconLabel;
    LevelBar? bar;
    Label? volumeLabel;

    public static AudioVolumeLevelWindow? New(AudioService? service)
    {
        if (service is null)
            return null;

        var output = service.DefaultOutput;
        if (output is null)
            return null;

        var window = NewWithProperties([]);

        output.Changed += (e) =>
        {
            if (e is null)
                return;

            e.Volume.Changed += (v) =>
            {
                window.Update(v, e.IsMuted.Value);
            };

            e.IsMuted.Changed += (m) =>
            {
                window.Update(e.Volume.Value, m);
            };
        };

        output
            .Value
            ?.Volume.Changed += (v) =>
            {
                window.Update(v, output.Value.IsMuted.Value);
            };

        output
            .Value
            ?.IsMuted.Changed += (m) =>
            {
                window.Update(output.Value.Volume.Value, m);
            };

        window.SetChild(window.CreateContent(output.Value));
        return window;
    }

    partial void Initialize()
    {
        AddCssClass("volume-window");
        Title = "volume";
        DefaultHeight = 50;
        DefaultWidth = 160;
        Opacity = 0.75;
        MarginBottom = 80;
        Modal = true;
        Resizable = false;
        Visible = false;
        Focusable = false;
        CanFocus = false;
        Hexpand = false;
        Vexpand = false;
        CanTarget = false;
        FocusOnClick = false;

        LayerShell.InitForWindow(this);
        LayerShell.SetLayer(this, Layer.Overlay);
        LayerShell.SetNamespace(this, "hyprbar");

        // Anchors
        LayerShell.SetAnchor(this, Edge.Left, false);
        LayerShell.SetAnchor(this, Edge.Right, false);
        LayerShell.SetAnchor(this, Edge.Bottom, true);
        LayerShell.SetAnchor(this, Edge.Top, false);

        LayerShell.SetKeyboardMode(this, KeyboardMode.None);

        // Set exlusive so other windows won't hide beneath it
        // LayerShell.SetAutoExclusiveZone(window);

        // Margins
        LayerShell.SetMargin(this, Edge.Bottom, 80);
    }

    private void Update(double volume, bool isMuted)
    {
        iconLabel?.SetText(GetVolumeIcon(volume, isMuted));
        bar?.SetValue(volume);
        volumeLabel?.SetText($"{(int)(volume * 100)}");
    }

    [MemberNotNull(nameof(iconLabel))]
    [MemberNotNull(nameof(bar))]
    [MemberNotNull(nameof(volumeLabel))]
    private Box CreateContent(PipeWireNode? node)
    {
        double volume = 0;
        bool isMuted = true;

        if (node is not null)
        {
            volume = node.Volume;
            isMuted = node.IsMuted;
        }

        Box box = Box.New(Orientation.Horizontal, 10);
        box.HeightRequest = 10;

        iconLabel = Label.New(GetVolumeIcon(volume, isMuted));
        iconLabel.MarginStart = 10;
        iconLabel.MarginEnd = 10;
        iconLabel.AddCssClass("volume-level-icon");
        box.Append(iconLabel);

        bar = LevelBar.New();
        bar.Vexpand = false;
        bar.Hexpand = true;
        bar.HeightRequest = 10;
        bar.MinValue = 0;
        bar.MaxValue = 1;
        bar.Value = volume;
        bar.AddCssClass("volume-level-bar");

        // DODAĆ CSS
        bar.AddOffsetValue("verylow", 0.15);
        bar.AddOffsetValue(Gtk.Constants.LEVEL_BAR_OFFSET_LOW, 0.25);
        bar.AddOffsetValue("mid", 0.75);
        bar.AddOffsetValue(Gtk.Constants.LEVEL_BAR_OFFSET_HIGH, 0.85);
        bar.AddOffsetValue(Gtk.Constants.LEVEL_BAR_OFFSET_FULL, 1);

        box.Append(bar);

        volumeLabel = Label.New($"{(int)(volume * 100)}");
        volumeLabel.WidthChars = 3;
        volumeLabel.MarginEnd = 10;
        volumeLabel.AddCssClass("volume-level-text");
        box.Append(volumeLabel);

        return box;
    }

    private static string GetVolumeIcon(double volume, bool isMuted)
    {
        if (isMuted)
            return Icons.Audio.VolumeOff;
        if (volume < 0.3)
            return Icons.Audio.VolumeLow;
        if (volume < 0.7)
            return Icons.Audio.VolumeMedium;
        return Icons.Audio.VolumeHigh;
    }
}
