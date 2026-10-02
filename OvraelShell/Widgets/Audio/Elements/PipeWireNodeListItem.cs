using System.Diagnostics.CodeAnalysis;
using Gtk;
using OvraelShell.InterfaceElements;
using OvraelShell.Models.Audio.PipeWire;
using OvraelShell.Services;

namespace OvraelShell.Widgets.Audio.Elements;

[GObject.Subclass<Gtk.Box>]
public sealed partial class PipeWireNodeListItem
{
    private PipeWireNode? node;

    // Knows the links - which sink a stream plays on
    private AudioService? service;

    // While dragging, PipeWire reports back the earlier values - they would pull the slider back
    private const long UserChangeHoldMs = 500;

    private Label iconLabel;
    private Label nameLabel;
    private Label detailsLabel;

    // Hidden until the volume of the node is known
    private Box volumeRow;
    private Button muteButton;
    private Scale volumeScale;

    // Outputs and inputs only - moves all streams of their direction to them
    private Button moveStreamsButton;

    // Set while the slider is moved from code, so it does not set the volume back
    private bool updatingVolume;
    private long lastUserChange;

    // Streams only - the output they play on (or input they record from)
    private DropDown targetDropDown;

    // Choices of the drop-down: null for the default, then the outputs
    private readonly List<PipeWireNode?> targetOptions = [];
    private string[] targetNames = [];

    // Set while the choice is changed from code, so it does not move the stream back
    private bool updatingTarget;

    public static PipeWireNodeListItem New()
    {
        return NewWithProperties([]);
    }

    partial void Initialize()
    {
        SetOrientation(Orientation.Horizontal);
        SetMarginTop(8);
        SetMarginBottom(8);
        SetMarginStart(12);
        SetMarginEnd(12);
        SetSpacing(10);

        AppendContent();
        AddCssClass("audio-node-listitem");
    }

    [MemberNotNull(
        nameof(iconLabel),
        nameof(nameLabel),
        nameof(detailsLabel),
        nameof(volumeRow),
        nameof(muteButton),
        nameof(volumeScale),
        nameof(moveStreamsButton),
        nameof(targetDropDown)
    )]
    private void AppendContent()
    {
        iconLabel = Label.New(string.Empty);

        nameLabel = Label.New(string.Empty);
        nameLabel.SetXalign(0);
        nameLabel.SetWidthChars(40);
        nameLabel.SetMaxWidthChars(40);
        nameLabel.SetEllipsize(Pango.EllipsizeMode.End);

        detailsLabel = Label.New(string.Empty);
        detailsLabel.SetXalign(0);
        detailsLabel.SetMaxWidthChars(40);
        detailsLabel.SetEllipsize(Pango.EllipsizeMode.End);
        detailsLabel.AddCssClass("audio-listitem-details");

        muteButton = Button.NewWithLabel(string.Empty);
        muteButton.SetCursor(Cursors.Pointer);
        muteButton.OnClicked += (_, _) => OnMuteClicked();

        // Percent, like wpctl and pavucontrol show it
        volumeScale = Scale.NewWithRange(Orientation.Horizontal, 0, 100, 1);
        volumeScale.SetDigits(0);
        volumeScale.SetDrawValue(true);
        volumeScale.SetValuePos(PositionType.Right);
        volumeScale.Hexpand = true;
        volumeScale.OnValueChanged += (_, _) => OnVolumeMoved();

        moveStreamsButton = Button.NewWithLabel(Icons.Audio.MoveStreamsHere);
        moveStreamsButton.SetCursor(Cursors.Pointer);
        moveStreamsButton.OnClicked += (_, _) => OnMoveStreamsClicked();

        volumeRow = Box.New(Orientation.Horizontal, 6);
        volumeRow.Append(muteButton);
        volumeRow.Append(volumeScale);
        volumeRow.Append(moveStreamsButton);
        volumeRow.Visible = false;

        targetDropDown = DropDown.NewFromStrings([]);
        targetDropDown.SetHalign(Align.Start);
        targetDropDown.SetCursor(Cursors.Pointer);
        targetDropDown.TooltipText = "Plays on";
        targetDropDown.Visible = false;

        // Long output names would widen the popover - the button cuts them, the list shows them
        targetDropDown.SetFactory(CreateTargetFactory(ellipsize: true));
        targetDropDown.SetListFactory(CreateTargetFactory(ellipsize: false));

        targetDropDown.OnNotify += (_, args) =>
        {
            if (args.Pspec.GetName() == "selected")
                OnTargetSelected();
        };

        var texts = Box.New(Orientation.Vertical, 2);
        texts.Hexpand = true;
        texts.Append(nameLabel);
        texts.Append(detailsLabel);
        texts.Append(targetDropDown);
        texts.Append(volumeRow);

        Append(iconLabel);
        Append(texts);
    }

    /// <summary>Shows <paramref name="node"/> and follows its changes until <see cref="Unbind"/>.</summary>
    public void Bind(PipeWireNode node, AudioService service)
    {
        Unbind();

        this.node = node;
        this.service = service;
        node.OnChange += OnNodeChanged;

        // Rows are recycled - the hold belongs to the previous node
        lastUserChange = 0;

        Refresh();
    }

    /// <summary>Stops following the node - otherwise it would update a recycled row.</summary>
    public void Unbind()
    {
        node?.OnChange -= OnNodeChanged;
        node = null;
    }

    private void OnNodeChanged(PipeWireNode _) => Refresh();

    private void Refresh()
    {
        if (node is null || service is null)
            return;

        iconLabel.SetLabel(AudioIcons.Node(node.MediaClass));
        nameLabel.SetLabel(node.DisplayName);
        detailsLabel.SetLabel(DetailsOf(node, service));
        UpdateVolume(node);
        UpdateTarget(node, service);

        TooltipText = string.Join(
            "\n",
            new[]
            {
                $"· Id: {node.GlobalId}",
                $"· Name: {node.Name}",
                $"· Media class: {node.MediaClass}",
                $"· Serial: {node.ObjectSerial}",
                node.ObjectPath is null ? null : $"· Object path: {node.ObjectPath}",
                node.DeviceId is null ? null : $"· Device id: {node.DeviceId}",
            }.OfType<string>()
        );
    }

    private void UpdateVolume(PipeWireNode node)
    {
        volumeRow.Visible = node.HasVolume;

        if (!node.HasVolume)
            return;

        var percent = Math.Round(node.Volume.Value * 100);

        muteButton.SetLabel(
            node.MediaClass
                is PipeWireMediaTypes.AudioSource
                    or PipeWireMediaTypes.StreamInputAudio
                ? AudioIcons.Microphone(node.IsMuted)
                : AudioIcons.Volume((uint)percent, node.IsMuted)
        );
        muteButton.TooltipText = node.IsMuted ? "Unmute" : "Mute";

        moveStreamsButton.Visible = !AudioService.IsStream(node);
        moveStreamsButton.TooltipText =
            node.MediaClass == PipeWireMediaTypes.AudioSource
                ? "Record all streams from this device"
                : "Play all streams on this device";

        if (Environment.TickCount64 - lastUserChange < UserChangeHoldMs)
            return;

        updatingVolume = true;
        volumeScale.SetValue(percent);
        updatingVolume = false;
    }

    private void OnVolumeMoved()
    {
        if (updatingVolume || node is null || service is null)
            return;

        lastUserChange = Environment.TickCount64;
        service.SetVolume(node, volumeScale.GetValue() / 100);
    }

    private void OnMoveStreamsClicked()
    {
        if (node is not null)
            service?.MoveStreamsTo(node);
    }

    private void OnMuteClicked()
    {
        if (node is not null)
            service?.SetMuted(node, !node.IsMuted.Value);
    }

    private void UpdateTarget(PipeWireNode node, AudioService service)
    {
        var isStream = AudioService.IsStream(node);
        targetDropDown.Visible = isStream;

        if (!isStream)
            return;

        targetOptions.Clear();
        targetOptions.Add(null);
        targetOptions.AddRange(service.TargetsFor(node));

        var defaultTarget = service.DefaultTargetFor(node);
        string[] names =
        [
            defaultTarget is null ? "Default" : $"Default ({defaultTarget.DisplayName})",
            .. targetOptions.Skip(1).Select(target => target!.DisplayName),
        ];

        // No target, or one that is gone - the stream follows the default
        var selected = Math.Max(0, targetOptions.IndexOf(service.ChosenTargetOf(node)));

        updatingTarget = true;

        // A new model drops the open list, so only when the choices really changed
        if (!names.SequenceEqual(targetNames))
        {
            targetDropDown.SetModel(StringList.New(names));
            targetNames = names;
        }

        targetDropDown.SetSelected((uint)selected);
        updatingTarget = false;
    }

    private void OnTargetSelected()
    {
        if (updatingTarget || node is null || service is null)
            return;

        var selected = targetDropDown.GetSelected();

        // GTK_INVALID_LIST_POSITION while the model is replaced
        if (selected >= targetOptions.Count)
            return;

        service.SetTarget(node, targetOptions[(int)selected]);
    }

    private static SignalListItemFactory CreateTargetFactory(bool ellipsize)
    {
        var factory = SignalListItemFactory.New();

        factory.OnSetup += (_, args) =>
        {
            var label = Label.New(string.Empty);
            label.SetXalign(0);

            if (ellipsize)
            {
                label.SetMaxWidthChars(34);
                label.SetEllipsize(Pango.EllipsizeMode.End);
            }

            ((ListItem)args.Object).SetChild(label);
        };

        factory.OnBind += (_, args) =>
        {
            var listItem = (ListItem)args.Object;

            if (listItem.GetChild() is Label label && listItem.GetItem() is StringObject name)
                label.SetLabel(name.GetString());
        };

        return factory;
    }

    /// <summary>What the node is and what it is linked to - e.g. "Playback → Built-in audio".</summary>
    private static string DetailsOf(PipeWireNode node, AudioService service) =>
        node.MediaClass switch
        {
            PipeWireMediaTypes.AudioSink => WithCount("Output", service.InputsOf(node), "playing"),
            PipeWireMediaTypes.AudioSource => WithCount(
                "Input",
                service.OutputsOf(node),
                "recording"
            ),
            PipeWireMediaTypes.StreamOutputAudio => WithTargets(
                "Playback",
                "→",
                service.OutputsOf(node)
            ),
            PipeWireMediaTypes.StreamInputAudio => WithTargets(
                "Recording",
                "←",
                service.InputsOf(node)
            ),
            _ => node.MediaClass ?? string.Empty,
        };

    private static string WithCount(string kind, IEnumerable<PipeWireNode> linked, string what)
    {
        var count = linked.Count();
        return count == 0 ? kind : $"{kind} · {count} {what}";
    }

    private static string WithTargets(string kind, string arrow, IEnumerable<PipeWireNode> linked)
    {
        var names = string.Join(", ", linked.Select(target => target.DisplayName));
        return names.Length == 0 ? kind : $"{kind} {arrow} {names}";
    }
}
