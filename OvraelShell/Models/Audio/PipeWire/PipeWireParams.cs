using OvraelShell.Models.Audio.Spa;

namespace OvraelShell.Models.Audio.PipeWire;

/// <summary>Volume and mute in the Props param of nodes and the Route param of devices.</summary>
internal static class PipeWireParams
{
    // enum spa_param_type
    public const uint Props = 2;
    public const uint Route = 13;

    // SPA_TYPE_OBJECT_Props, SPA_TYPE_OBJECT_ParamRoute
    private const uint ObjectProps = 0x40002;
    private const uint ObjectRoute = 0x40009;

    // enum spa_prop
    private const uint PropMute = 0x10004;
    private const uint PropChannelVolumes = 0x10008;

    // enum spa_param_route
    private const uint RouteIndex = 1;
    private const uint RouteDevice = 3;
    private const uint RouteProps = 10;
    private const uint RouteSave = 13;

    /// <summary>Linear volume per channel and mute - either may be missing from a param.</summary>
    public readonly record struct VolumeState(float[]? ChannelVolumes, bool? Mute);

    /// <summary>
    /// An active route of a device - <paramref name="Device"/> is the card.profile.device
    /// of the node playing through it.
    /// </summary>
    public readonly record struct RouteInfo(int Index, int Device);

    public static VolumeState? ReadProps(byte[] pod)
    {
        var props = SpaPod.ReadObject(pod, ObjectProps);

        if (props is null)
            return null;

        return new VolumeState(
            SpaPod.ReadFloatArray(props.GetValueOrDefault(PropChannelVolumes)),
            SpaPod.ReadBool(props.GetValueOrDefault(PropMute))
        );
    }

    public static RouteInfo? ReadRoute(byte[] pod)
    {
        var props = SpaPod.ReadObject(pod, ObjectRoute);

        if (
            SpaPod.ReadInt(props?.GetValueOrDefault(RouteIndex)) is not { } index
            || SpaPod.ReadInt(props?.GetValueOrDefault(RouteDevice)) is not { } device
        )
            return null;

        return new RouteInfo(index, device);
    }

    /// <summary>Props of a node - sets only what is given.</summary>
    public static byte[] BuildProps(float[]? channelVolumes, bool? mute) =>
        SpaPod.Object(ObjectProps, Props, VolumeProps(channelVolumes, mute));

    /// <summary>
    /// Volume of a device route. Saved, so WirePlumber restores it after a restart -
    /// the same as pavucontrol and wpctl do.
    /// </summary>
    public static byte[] BuildRoute(RouteInfo route, float[]? channelVolumes, bool? mute) =>
        SpaPod.Object(
            ObjectRoute,
            Route,
            (RouteIndex, SpaPod.Int(route.Index)),
            (RouteDevice, SpaPod.Int(route.Device)),
            (RouteProps, SpaPod.Object(ObjectProps, Route, VolumeProps(channelVolumes, mute))),
            (RouteSave, SpaPod.Bool(true))
        );

    private static (uint, byte[])[] VolumeProps(float[]? channelVolumes, bool? mute)
    {
        var props = new List<(uint, byte[])>();

        if (channelVolumes is not null)
            props.Add((PropChannelVolumes, SpaPod.FloatArray(channelVolumes)));

        if (mute is not null)
            props.Add((PropMute, SpaPod.Bool(mute.Value)));

        return [.. props];
    }
}
