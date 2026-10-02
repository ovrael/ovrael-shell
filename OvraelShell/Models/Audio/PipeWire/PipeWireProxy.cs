using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OvraelShell.Models.Audio.Spa;

namespace OvraelShell.Models.Audio.PipeWire;

/// <summary>
/// A node or device bound from the registry - gives its params and full props, and sets params.
/// Created by <see cref="PipeWireConnection.Bind"/>. Events are raised on the GLib main thread.
/// </summary>
internal sealed unsafe partial class PipeWireProxy : IDisposable, IPipeWireBinding
{
    private const string Library = "libpipewire-0.3.so.0";

    // PW_VERSION_NODE, PW_VERSION_DEVICE
    private const uint ObjectVersion = 3;

    // PW_VERSION_NODE_EVENTS, PW_VERSION_DEVICE_EVENTS
    private const uint EventsVersion = 0;

    private readonly PipeWireConnection connection;
    private readonly bool isNode;
    private readonly IntPtr proxy;

    // PipeWire keeps pointers to both, so they live in native memory until Destroy
    private readonly ObjectEvents* events;
    private readonly SpaHook* listener;

    private GCHandle self;
    private bool destroyed;

    public uint GlobalId { get; }

    /// <summary>Full props of a node - the registry announces only a part of them.</summary>
    public event Action<IReadOnlyDictionary<string, string>>? InfoChanged;

    /// <summary>A subscribed param changed - its id and the whole pod.</summary>
    public event Action<uint, byte[]>? ParamChanged;

    /// <summary>Called with the loop locked.</summary>
    internal PipeWireProxy(PipeWireConnection connection, IntPtr registry, uint id, string type)
    {
        this.connection = connection;
        GlobalId = id;
        isNode = type == PipeWireInterfaces.Node;

        if (!isNode && type != PipeWireInterfaces.Device)
            throw new ArgumentException($"Only nodes and devices can be bound, not {type}.");

        proxy = pw_registry_bind(registry, id, type, ObjectVersion, 0);
        if (proxy == IntPtr.Zero)
            throw new Exception($"Failed to bind PipeWire object {id}.");

        self = GCHandle.Alloc(this);

        events = (ObjectEvents*)NativeMemory.AllocZeroed((nuint)sizeof(ObjectEvents));
        events->Version = EventsVersion;
        events->Param = &OnParam;

        // Device info has a different layout and nothing needed here, so it is not read
        if (isNode)
            events->Info = &OnNodeInfo;

        listener = (SpaHook*)NativeMemory.AllocZeroed((nuint)sizeof(SpaHook));

        if (isNode)
            pw_node_add_listener(proxy, listener, events, GCHandle.ToIntPtr(self));
        else
            pw_device_add_listener(proxy, listener, events, GCHandle.ToIntPtr(self));
    }

    /// <summary>Asks for <paramref name="ids"/> now and again on every change.</summary>
    public void SubscribeParams(params uint[] ids)
    {
        using var _ = connection.Lock();

        if (destroyed)
            return;

        fixed (uint* idsPointer = ids)
        {
            if (isNode)
                pw_node_subscribe_params(proxy, idsPointer, (uint)ids.Length);
            else
                pw_device_subscribe_params(proxy, idsPointer, (uint)ids.Length);
        }
    }

    public void SetParam(uint id, byte[] pod)
    {
        using var _ = connection.Lock();

        if (destroyed)
            return;

        fixed (byte* podPointer = pod)
        {
            var result = isNode
                ? pw_node_set_param(proxy, id, 0, podPointer)
                : pw_device_set_param(proxy, id, 0, podPointer);

            if (result < 0)
                Console.WriteLine($"Setting param {id} of PipeWire object {GlobalId} failed: {result}");
        }
    }

    public void Dispose() => connection.Release(this);

    public void Destroy()
    {
        if (destroyed)
            return;

        destroyed = true;

        PipeWireConnection.RemoveHook(listener);
        pw_proxy_destroy(proxy);

        NativeMemory.Free(events);
        NativeMemory.Free(listener);
        self.Free();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnNodeInfo(IntPtr data, IntPtr info)
    {
        // An exception thrown into native code kills the process
        try
        {
            var nodeInfo = (NodeInfo*)info;

            // Info also comes for state and port changes
            if ((nodeInfo->ChangeMask & NodeInfo.ChangeMaskProps) == 0)
                return;

            var target = (PipeWireProxy)GCHandle.FromIntPtr(data).Target!;

            // Valid only during this callback - copy it
            var props = PipeWireConnection.ReadDict(nodeInfo->Props);

            PipeWireConnection.RunOnMainThread(() =>
            {
                if (!target.destroyed)
                    target.InfoChanged?.Invoke(props);
            });
        }
        catch (Exception exception)
        {
            Console.WriteLine($"PipeWire node info callback failed: {exception}");
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnParam(IntPtr data, int seq, uint id, uint index, uint next, IntPtr param)
    {
        try
        {
            if (param == IntPtr.Zero)
                return;

            var target = (PipeWireProxy)GCHandle.FromIntPtr(data).Target!;

            // Header (body size, type) plus the body - valid only during this callback
            var pod = new byte[8 + *(uint*)param];
            Marshal.Copy(param, pod, 0, pod.Length);

            PipeWireConnection.RunOnMainThread(() =>
            {
                if (!target.destroyed)
                    target.ParamChanged?.Invoke(id, pod);
            });
        }
        catch (Exception exception)
        {
            Console.WriteLine($"PipeWire param callback failed: {exception}");
        }
    }

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr pw_registry_bind(
        IntPtr registry,
        uint id,
        string type,
        uint version,
        nuint userDataSize
    );

    [LibraryImport(Library)]
    private static partial int pw_node_add_listener(
        IntPtr node,
        SpaHook* listener,
        ObjectEvents* events,
        IntPtr data
    );

    [LibraryImport(Library)]
    private static partial int pw_node_subscribe_params(IntPtr node, uint* ids, uint count);

    [LibraryImport(Library)]
    private static partial int pw_node_set_param(IntPtr node, uint id, uint flags, byte* param);

    [LibraryImport(Library)]
    private static partial int pw_device_add_listener(
        IntPtr device,
        SpaHook* listener,
        ObjectEvents* events,
        IntPtr data
    );

    [LibraryImport(Library)]
    private static partial int pw_device_subscribe_params(IntPtr device, uint* ids, uint count);

    [LibraryImport(Library)]
    private static partial int pw_device_set_param(IntPtr device, uint id, uint flags, byte* param);

    [LibraryImport(Library)]
    private static partial void pw_proxy_destroy(IntPtr proxy);
}
