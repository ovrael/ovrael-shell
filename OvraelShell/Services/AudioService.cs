using System.Text.Json;
using OvraelShell.Interfaces;
using OvraelShell.Models;
using OvraelShell.Models.Audio.PipeWire;

namespace OvraelShell.Services;

public sealed class AudioService : IDisposable
{
    private readonly PipeWireConnection pipeWire;

    // Audio nodes by PipeWire global id - sinks, sources and streams
    public readonly ReactiveCollection<PipeWireNode> Nodes = new();

    // Audio devices (sound cards) by PipeWire global id
    public readonly ReactiveCollection<PipeWireDevice> Devices = new();

    // Links between audio nodes - which stream plays on which sink
    public readonly ReactiveCollection<PipeWireLink> Links = new();

    // Bound nodes and devices by global id - they give and take the volume
    private readonly Dictionary<uint, PipeWireProxy> proxies = new();

    // Metadata of WirePlumber with the default devices and the targets of streams
    private const string DefaultMetadataName = "default";
    private const string TargetObjectKey = "target.object";

    // Older key with a node id - cleared when moving, so it does not win over target.object
    private const string TargetNodeKey = "target.node";
    private const string DefaultSinkKey = "default.audio.sink";
    private const string DefaultSourceKey = "default.audio.source";

    private PipeWireMetadata? defaultMetadata;

    // target.object of streams by stream id - a serial or a name of the node
    private readonly Dictionary<uint, string> streamTargets = new();

    // node.name of the default output and input
    private string? defaultSinkName;
    private string? defaultSourceName;

    public ReactiveProperty<AudioDevice?> ActiveDevice { get; set; } = new();

    public event Action<AudioService>? OnChange;

    public AudioService()
    {
        Links.Added += NotifyLinkedNodes;
        Links.Removed += NotifyLinkedNodes;

        // Streams offer the outputs and inputs as their targets
        Nodes.Added += OnTargetsChanged;
        Nodes.Removed += OnTargetsChanged;

        pipeWire = new PipeWireConnection();

        pipeWire.GlobalAdded += OnGlobalAdded;
        pipeWire.GlobalRemoved += OnGlobalRemoved;
    }

    private void OnGlobalAdded(PipeWireGlobal global)
    {
        string type = global.Type;
        switch (type)
        {
            case PipeWireInterfaces.Node:
                OnNodeAdded(global);
                break;

            case PipeWireInterfaces.Device:
                OnDeviceAdded(global);
                break;

            case PipeWireInterfaces.Link:
                OnLinkAdded(global);
                break;

            case PipeWireInterfaces.Metadata:
                OnMetadataAdded(global);
                break;

            default:
                break;
        }
    }

    private void OnNodeAdded(PipeWireGlobal global)
    {
        var node = PipeWireNode.Create(global);

        switch (node.MediaClass)
        {
            case PipeWireMediaTypes.AudioSink:
            case PipeWireMediaTypes.AudioSource:
            case PipeWireMediaTypes.StreamOutputAudio:
            case PipeWireMediaTypes.StreamInputAudio:
                Nodes.Add(node);
                BindNode(node);
                break;
        }
    }

    private void OnDeviceAdded(PipeWireGlobal global)
    {
        var device = PipeWireDevice.Create(global);

        if (device.MediaClass != PipeWireMediaTypes.AudioDevice)
            return;

        Devices.Add(device);
        BindDevice(device);
    }

    #region Volume

    /// <summary>
    /// Follows the volume of a node - its Props param. Streams have no device, so their
    /// volume is set on the node too, and WirePlumber remembers it per application.
    /// </summary>
    private void BindNode(PipeWireNode node)
    {
        if (Bind(node.GlobalId, PipeWireInterfaces.Node) is not { } proxy)
            return;

        // card.profile.device is only in the full props - it picks the route for SetVolume
        proxy.InfoChanged += props =>
            node.UpdateCardProfileDevice(
                int.TryParse(props.GetValueOrDefault("card.profile.device"), out var device)
                    ? device
                    : null
            );

        proxy.ParamChanged += (id, pod) =>
        {
            if (id == PipeWireParams.Props && PipeWireParams.ReadProps(pod) is { } state)
                node.UpdateVolume(state.ChannelVolumes, state.Mute);
        };

        proxy.SubscribeParams(PipeWireParams.Props);
    }

    /// <summary>Follows the active routes of a sound card - their volume is set in SetVolume.</summary>
    private void BindDevice(PipeWireDevice device)
    {
        if (Bind(device.GlobalId, PipeWireInterfaces.Device) is not { } proxy)
            return;

        proxy.ParamChanged += (id, pod) =>
        {
            if (id == PipeWireParams.Route && PipeWireParams.ReadRoute(pod) is { } route)
                device.RouteIndexByCardDevice[route.Device] = route.Index;
        };

        proxy.SubscribeParams(PipeWireParams.Route);
    }

    private PipeWireProxy? Bind(uint id, string type)
    {
        try
        {
            var proxy = pipeWire.Bind(id, type);
            proxies[id] = proxy;
            return proxy;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Cannot follow the volume of {id}: {exception.Message}");
            return null;
        }
    }

    /// <summary>
    /// Sets the volume on the scale of sliders (0-1, cubic). Keeps the balance between
    /// channels - the loudest one gets <paramref name="volume"/>.
    /// </summary>
    public void SetVolume(PipeWireNode node, double volume)
    {
        var linear = (float)Math.Pow(Math.Clamp(volume, 0, 1), 3);
        var current = node.ChannelVolumes;
        var loudest = current.Length == 0 ? 0 : current.Max();

        float[] channelVolumes =
            loudest > 0 ? [.. current.Select(channel => channel / loudest * linear)]
            : current.Length > 0 ? [.. current.Select(_ => linear)]
            : [linear];

        ApplyVolume(node, channelVolumes, null);
    }

    public void SetMuted(PipeWireNode node, bool muted) => ApplyVolume(node, null, muted);

    private void ApplyVolume(PipeWireNode node, float[]? channelVolumes, bool? mute)
    {
        // A hardware output keeps its volume on the device route - WirePlumber saves it there.
        // The node reports the route volume back in its Props, so the slider follows.
        if (FindRoute(node) is var (deviceProxy, route))
        {
            deviceProxy.SetParam(
                PipeWireParams.Route,
                PipeWireParams.BuildRoute(route, channelVolumes, mute)
            );
            return;
        }

        if (proxies.TryGetValue(node.GlobalId, out var nodeProxy))
            nodeProxy.SetParam(
                PipeWireParams.Props,
                PipeWireParams.BuildProps(channelVolumes, mute)
            );
    }

    private (PipeWireProxy, PipeWireParams.RouteInfo)? FindRoute(PipeWireNode node)
    {
        if (node.DeviceId is not { } deviceId || node.CardProfileDevice is not { } cardDevice)
            return null;

        var device = Devices.Items.FirstOrDefault(device => device.GlobalId == deviceId);

        if (
            device is null
            || !device.RouteIndexByCardDevice.TryGetValue(cardDevice, out var routeIndex)
            || !proxies.TryGetValue(deviceId, out var proxy)
        )
            return null;

        return (proxy, new PipeWireParams.RouteInfo(routeIndex, cardDevice));
    }

    #endregion

    private void OnLinkAdded(PipeWireGlobal global)
    {
        var link = new PipeWireLink(global);

        // Nodes are announced before their links, so MIDI and video links are skipped here
        if (FindNode(link.OutputNode) is null || FindNode(link.InputNode) is null)
            return;

        Links.Add(link);
    }

    /// <summary>
    /// Nodes that <paramref name="node"/> sends audio to - for a stream, the sinks it plays on.
    /// </summary>
    public IEnumerable<PipeWireNode> OutputsOf(PipeWireNode node) =>
        Links
            .Items.Where(link => link.OutputNode == node.GlobalId)
            // One link per channel - a stereo stream links to the same sink twice
            .Select(link => link.InputNode)
            .Distinct()
            .Select(FindNode)
            .OfType<PipeWireNode>();

    /// <summary>
    /// Nodes that send audio to <paramref name="node"/> - for a sink, the streams playing on it.
    /// </summary>
    public IEnumerable<PipeWireNode> InputsOf(PipeWireNode node) =>
        Links
            .Items.Where(link => link.InputNode == node.GlobalId)
            .Select(link => link.OutputNode)
            .Distinct()
            .Select(FindNode)
            .OfType<PipeWireNode>();

    // Links tell where a stream plays - the nodes on both ends show it
    private void NotifyLinkedNodes(PipeWireLink link)
    {
        FindNode(link.OutputNode)?.NotifyChanged();
        FindNode(link.InputNode)?.NotifyChanged();
    }

    private PipeWireNode? FindNode(uint id) =>
        Nodes.Items.FirstOrDefault(node => node.GlobalId == id);

    #region Targets of streams

    private void OnMetadataAdded(PipeWireGlobal global)
    {
        if (global.Prop("metadata.name") != DefaultMetadataName || defaultMetadata is not null)
            return;

        try
        {
            defaultMetadata = pipeWire.BindMetadata(global.Id);
            defaultMetadata.PropertyChanged += OnMetadataProperty;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Cannot follow the targets of streams: {exception.Message}");
        }
    }

    private void OnMetadataProperty(uint subject, string? key, string? type, string? value)
    {
        switch (key)
        {
            // All properties of the subject were removed
            case null:
                if (streamTargets.Remove(subject))
                    FindNode(subject)?.NotifyChanged();
                break;

            case TargetObjectKey:
                if (value is null)
                    streamTargets.Remove(subject);
                else
                    streamTargets[subject] = value;

                FindNode(subject)?.NotifyChanged();
                break;

            case DefaultSinkKey:
                defaultSinkName = NameFromJson(value);
                NotifyStreams();
                break;

            case DefaultSourceKey:
                defaultSourceName = NameFromJson(value);
                NotifyStreams();
                break;
        }
    }

    // Default devices are stored as {"name":"alsa_output..."}
    private static string? NameFromJson(string? value)
    {
        if (value is null)
            return null;

        try
        {
            using var json = JsonDocument.Parse(value);
            return json.RootElement.TryGetProperty("name", out var name) ? name.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void OnTargetsChanged(PipeWireNode node)
    {
        if (node.MediaClass is PipeWireMediaTypes.AudioSink or PipeWireMediaTypes.AudioSource)
            NotifyStreams();
    }

    private void NotifyStreams()
    {
        foreach (var node in Nodes.Items.Where(IsStream))
            node.NotifyChanged();
    }

    public static bool IsStream(PipeWireNode node) =>
        node.MediaClass
            is PipeWireMediaTypes.StreamOutputAudio
                or PipeWireMediaTypes.StreamInputAudio;

    /// <summary>Outputs a playback stream can play on, or inputs a recording one can record from.</summary>
    public IEnumerable<PipeWireNode> TargetsFor(PipeWireNode stream)
    {
        var targetClass =
            stream.MediaClass == PipeWireMediaTypes.StreamInputAudio
                ? PipeWireMediaTypes.AudioSource
                : PipeWireMediaTypes.AudioSink;

        return Nodes.Items.Where(node => node.MediaClass == targetClass);
    }

    /// <summary>The default output (or input) - where a stream without a target goes.</summary>
    public PipeWireNode? DefaultTargetFor(PipeWireNode stream)
    {
        var name =
            stream.MediaClass == PipeWireMediaTypes.StreamInputAudio
                ? defaultSourceName
                : defaultSinkName;

        return TargetsFor(stream).FirstOrDefault(node => node.Name == name);
    }

    /// <summary>
    /// The output chosen for the stream - null when it follows the default.
    /// WirePlumber sets it too, when it restores where an application played last time.
    /// </summary>
    public PipeWireNode? ChosenTargetOf(PipeWireNode stream)
    {
        if (!streamTargets.TryGetValue(stream.GlobalId, out var target))
            return null;

        // A serial, as set by pipewire-pulse and WirePlumber, or a node name
        return TargetsFor(stream)
            .FirstOrDefault(node => node.ObjectSerial == target || node.Name == target);
    }

    /// <summary>
    /// Moves the stream to <paramref name="target"/>, or back to the default output when null.
    /// WirePlumber relinks it and remembers the choice for the application.
    /// </summary>
    public void SetTarget(PipeWireNode stream, PipeWireNode? target)
    {
        if (defaultMetadata is null)
        {
            Console.WriteLine($"Cannot move {stream.DisplayName} - no default metadata");
            return;
        }

        // The same as pipewire-pulse does when pavucontrol moves a stream
        defaultMetadata.SetProperty(stream.GlobalId, TargetNodeKey, null, null);
        defaultMetadata.SetProperty(
            stream.GlobalId,
            TargetObjectKey,
            target is null ? null : "Spa:Id",
            target?.ObjectSerial
        );
    }

    /// <summary>
    /// Moves every stream of the same direction to <paramref name="target"/> -
    /// all playback to an output, or all recording from an input.
    /// </summary>
    public void MoveStreamsTo(PipeWireNode target)
    {
        foreach (var stream in Nodes.Items.Where(IsStream).ToList())
        {
            if (TargetsFor(stream).Contains(target))
                SetTarget(stream, target);
        }
    }

    #endregion

    private void OnGlobalRemoved(uint id)
    {
        if (proxies.Remove(id, out var proxy))
            proxy.Dispose();

        if (defaultMetadata?.GlobalId == id)
        {
            defaultMetadata.Dispose();
            defaultMetadata = null;
        }

        streamTargets.Remove(id);

        var byGlobalId = (PipeWireBase t) => t.GlobalId == id;
        // Ids are unique across all globals, so only one of these can match
        if (
            Nodes.RemoveWhere(byGlobalId)
            || Devices.RemoveWhere(byGlobalId)
            || Links.RemoveWhere(link => link.GlobalId == id)
        )
            return;
    }

    public void Dispose()
    {
        pipeWire.GlobalAdded -= OnGlobalAdded;
        pipeWire.GlobalRemoved -= OnGlobalRemoved;

        foreach (var proxy in proxies.Values)
            proxy.Dispose();

        proxies.Clear();
        defaultMetadata?.Dispose();
        pipeWire.Dispose();
    }
}
