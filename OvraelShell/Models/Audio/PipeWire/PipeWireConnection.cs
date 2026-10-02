using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OvraelShell.Models.Audio.Spa;

namespace OvraelShell.Models.Audio.PipeWire;

/// <summary>
/// Connection to PipeWire running on its own thread loop.
/// Registry events are raised on the GLib main thread.
/// </summary>
internal sealed unsafe partial class PipeWireConnection : IDisposable
{
    private const string Library = "libpipewire-0.3.so.0";

    public const uint RegistryVersion = 3;
    private const uint RegistryEventsVersion = 0;

    private readonly IntPtr threadLoop;
    private readonly IntPtr context;
    private readonly IntPtr core;
    private readonly IntPtr registry;

    // PipeWire keeps pointers to both, so they live in native memory until Dispose
    private readonly RegistryEvents* registryEvents;
    private readonly SpaHook* registryListener;

    // Bound objects - destroyed before the core, which would destroy them itself
    private readonly HashSet<IPipeWireBinding> bindings = [];

    private GCHandle self;
    private bool disposed;

    public event Action<PipeWireGlobal>? GlobalAdded;
    public event Action<uint>? GlobalRemoved;

    public PipeWireConnection()
    {
        pw_init(IntPtr.Zero, IntPtr.Zero);

        threadLoop = pw_thread_loop_new("ovrael-pipewire", IntPtr.Zero);
        if (threadLoop == IntPtr.Zero)
            throw new Exception("Failed to create PipeWire thread loop.");

        context = pw_context_new(pw_thread_loop_get_loop(threadLoop), IntPtr.Zero, 0);
        if (context == IntPtr.Zero)
            throw new Exception("Failed to create PipeWire context.");

        core = pw_context_connect(context, IntPtr.Zero, 0);
        if (core == IntPtr.Zero)
            throw new Exception("Failed to connect to PipeWire.");

        registry = pw_core_get_registry(core, RegistryVersion, 0);
        if (registry == IntPtr.Zero)
            throw new Exception("Failed to get PipeWire registry.");

        self = GCHandle.Alloc(this);

        registryEvents = (RegistryEvents*)NativeMemory.AllocZeroed((nuint)sizeof(RegistryEvents));
        registryEvents->Version = RegistryEventsVersion;
        registryEvents->Global = &OnGlobal;
        registryEvents->GlobalRemove = &OnGlobalRemove;

        registryListener = (SpaHook*)NativeMemory.AllocZeroed((nuint)sizeof(SpaHook));

        pw_registry_add_listener(
            registry,
            registryListener,
            registryEvents,
            GCHandle.ToIntPtr(self)
        );

        // The loop is not running yet, so nothing above needs pw_thread_loop_lock
        if (pw_thread_loop_start(threadLoop) < 0)
            throw new Exception("Failed to start PipeWire thread loop.");
    }

    /// <summary>
    /// Binds a node or device from the registry, to read and set its params.
    /// Dispose the proxy when the global is removed.
    /// </summary>
    public PipeWireProxy Bind(uint id, string type)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        using var _ = Lock();

        var proxy = new PipeWireProxy(this, registry, id, type);
        bindings.Add(proxy);

        return proxy;
    }

    /// <summary>Binds a metadata object from the registry. Dispose it when the global is removed.</summary>
    public PipeWireMetadata BindMetadata(uint id)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        using var _ = Lock();

        var metadata = new PipeWireMetadata(this, registry, id);
        bindings.Add(metadata);

        return metadata;
    }

    /// <summary>
    /// Locks the loop thread - needed for every call on a proxy from another thread.
    /// Recursive, so it may be nested.
    /// </summary>
    internal LoopLock Lock() => new(threadLoop, disposed);

    internal void Release(IPipeWireBinding binding)
    {
        if (!bindings.Remove(binding))
            return;

        using var _ = Lock();
        binding.Destroy();
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;

        // Stopping joins the loop thread - no callbacks run after this
        pw_thread_loop_stop(threadLoop);

        foreach (var binding in bindings)
            binding.Destroy();

        bindings.Clear();

        RemoveHook(registryListener);
        pw_proxy_destroy(registry);
        pw_core_disconnect(core);
        pw_context_destroy(context);
        pw_thread_loop_destroy(threadLoop);

        NativeMemory.Free(registryEvents);
        NativeMemory.Free(registryListener);
        self.Free();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnGlobal(
        IntPtr data,
        uint id,
        uint permissions,
        IntPtr type,
        uint version,
        SpaDict* props
    )
    {
        // An exception thrown into native code kills the process
        try
        {
            var pipeWire = (PipeWireConnection)GCHandle.FromIntPtr(data).Target!;

            // props and type are valid only during this callback - copy them
            var global = new PipeWireGlobal(
                id,
                Marshal.PtrToStringUTF8(type) ?? string.Empty,
                version,
                ReadDict(props)
            );

            RunOnMainThread(() => pipeWire.GlobalAdded?.Invoke(global));
        }
        catch (Exception exception)
        {
            Console.WriteLine($"PipeWire global callback failed: {exception}");
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnGlobalRemove(IntPtr data, uint id)
    {
        try
        {
            var pipeWire = (PipeWireConnection)GCHandle.FromIntPtr(data).Target!;

            RunOnMainThread(() => pipeWire.GlobalRemoved?.Invoke(id));
        }
        catch (Exception exception)
        {
            Console.WriteLine($"PipeWire global remove callback failed: {exception}");
        }
    }

    internal static void RunOnMainThread(Action action) =>
        GLib.Functions.IdleAdd(
            GLib.Constants.PRIORITY_DEFAULT,
            () =>
            {
                action();
                return false;
            }
        );

    internal static Dictionary<string, string> ReadDict(SpaDict* dict)
    {
        var result = new Dictionary<string, string>();

        if (dict == null)
            return result;

        for (var i = 0; i < dict->ItemCount; i++)
        {
            var key = Marshal.PtrToStringUTF8(dict->Items[i].Key);

            if (key is not null)
                result[key] = Marshal.PtrToStringUTF8(dict->Items[i].Value) ?? string.Empty;
        }

        return result;
    }

    // spa_hook_remove is an inline function in the headers, so it is not exported
    internal static void RemoveHook(SpaHook* hook)
    {
        if (hook->LinkNext == null)
            return;

        hook->LinkPrev->Next = hook->LinkNext;
        hook->LinkNext->Prev = hook->LinkPrev;
        hook->LinkNext = null;
        hook->LinkPrev = null;
    }

    [LibraryImport(Library)]
    private static partial void pw_init(IntPtr argc, IntPtr argv);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr pw_thread_loop_new(string name, IntPtr properties);

    [LibraryImport(Library)]
    private static partial IntPtr pw_thread_loop_get_loop(IntPtr loop);

    [LibraryImport(Library)]
    private static partial int pw_thread_loop_start(IntPtr loop);

    [LibraryImport(Library)]
    private static partial void pw_thread_loop_stop(IntPtr loop);

    [LibraryImport(Library)]
    private static partial void pw_thread_loop_lock(IntPtr loop);

    [LibraryImport(Library)]
    private static partial void pw_thread_loop_unlock(IntPtr loop);

    [LibraryImport(Library)]
    private static partial void pw_thread_loop_destroy(IntPtr loop);

    [LibraryImport(Library)]
    private static partial IntPtr pw_context_new(
        IntPtr loop,
        IntPtr properties,
        nuint userDataSize
    );

    [LibraryImport(Library)]
    private static partial void pw_context_destroy(IntPtr context);

    [LibraryImport(Library)]
    private static partial IntPtr pw_context_connect(
        IntPtr context,
        IntPtr properties,
        nuint userDataSize
    );

    [LibraryImport(Library)]
    private static partial int pw_core_disconnect(IntPtr core);

    [LibraryImport(Library)]
    private static partial IntPtr pw_core_get_registry(
        IntPtr core,
        uint version,
        nuint userDataSize
    );

    [LibraryImport(Library)]
    private static partial int pw_registry_add_listener(
        IntPtr registry,
        SpaHook* listener,
        RegistryEvents* events,
        IntPtr data
    );

    [LibraryImport(Library)]
    private static partial void pw_proxy_destroy(IntPtr proxy);

    /// <summary>Holds the loop lock until disposed. Does nothing once the loop is stopped.</summary>
    internal readonly ref struct LoopLock
    {
        private readonly IntPtr loop;

        public LoopLock(IntPtr loop, bool stopped)
        {
            this.loop = stopped ? IntPtr.Zero : loop;

            if (this.loop != IntPtr.Zero)
                pw_thread_loop_lock(this.loop);
        }

        public void Dispose()
        {
            if (loop != IntPtr.Zero)
                pw_thread_loop_unlock(loop);
        }
    }
}
