using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;

namespace OvraelShell.Widgets.Elements.Network;

[GObject.Subclass<Gtk.Box>]
public sealed partial class TrafficContainer
{
    private Label iconLabel;
    private Label speedLabel;
    private Label unitLabel;

    public static TrafficContainer New(string icon)
    {
        var trafficContainer = NewWithProperties([]);
        trafficContainer.iconLabel.SetLabel(icon);
        return trafficContainer;
    }

    [MemberNotNull(nameof(iconLabel), nameof(speedLabel), nameof(unitLabel))]
    partial void Initialize()
    {
        iconLabel = CreateIconLabel();
        speedLabel = CreateSpeedLabel();
        unitLabel = CreateUnitLabel();

        Append(iconLabel);
        Append(speedLabel);
        Append(unitLabel);
    }

    private Label CreateIconLabel()
    {
        var iconLabel = Label.New(string.Empty);
        iconLabel.SetXalign(0);
        iconLabel.SetWidthChars(1);
        return iconLabel;
    }

    private Label CreateSpeedLabel()
    {
        var speedLabel = Label.New(string.Empty);
        speedLabel.SetXalign(1);
        speedLabel.SetWidthChars(8);
        speedLabel.AddCssClass("traffic-label");
        return speedLabel;
    }

    private Label CreateUnitLabel()
    {
        var unitLabel = Label.New(string.Empty);
        unitLabel.SetXalign(1);
        unitLabel.SetWidthChars(4);
        return unitLabel;
    }

    public void UpdateLabels(string speed, string unit)
    {
        speedLabel.SetLabel(speed);
        unitLabel.SetLabel(unit);
    }

    internal void SetEmpty()
    {
        speedLabel.SetLabel(string.Empty);
        unitLabel.SetLabel(string.Empty);
    }
}
