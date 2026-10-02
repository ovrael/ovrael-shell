using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OvraelShell.Models.Audio.Spa;

namespace OvraelShell.Models.Audio.PipeWire;

/// <summary>
/// A metadata object bound from the registry - key/value properties per subject (global id).
/// WirePlumber keeps the default devices and the targets of streams in the "default" one.
/// Created by <see cref="PipeWireConnection.BindMetadata"/>. Events are raised on the GLib main thread.
/// </summary>
internal sealed unsafe partial class PipeWireMetadata : IDisposable, IPipeWireBinding
{
    private const string Library = "libpipewire-0.3.so.0";

    // PW_VERSION_METADATA, PW_VERSION_METADATA_EVENTS
    private const uint ObjectVersion = 3;
    private const uint EventsVersion = 0;

    private readonly PipeWireConnection connection;
    private readonly IntPtr proxy;

    // PipeWire keeps pointers to both, so they live in native memory until Destroy
    private readonly MetadataEvents* events;
    private readonly SpaHook* listener;

    private GCHandle self;
    private bool destroyed;

    public uint GlobalId { get; }

    /// <summary>
    /// Subject, key, type and value of a property. Key null: all properties of the subject
    /// were removed. Value null: the property was removed. Comes for every existing
    /// property right after binding.
    /// </summary>
    public event Action<uint, string?, string?, string?>? PropertyChanged;

    /// <summary>Called with the loop locked.</summary>
    internal PipeWireMetadata(PipeWireConnection connection, IntPtr registry, uint id)
    {
        this.connection = connection;
        GlobalId = id;

        proxy = pw_registry_bind(registry, id, PipeWireInterfaces.Metadata, ObjectVersion, 0);
        if (proxy == IntPtr.Zero)
            throw new Exception($"Failed to bind PipeWire metadata {id}.");

        self = GCHandle.Alloc(this);

        events = (MetadataEvents*)NativeMemory.AllocZeroed((nuint)sizeof(MetadataEvents));
        events->Version = EventsVersion;
        events->Property = &OnProperty;

        listener = (SpaHook*)NativeMemory.AllocZeroed((nuint)sizeof(SpaHook));

        var result = Methods->AddListener(Interface->Data, listener, events, GCHandle.ToIntPtr(self));
        if (result < 0)
            throw new Exception($"Failed to listen to PipeWire metadata {id}: {result}");
    }

    // The proxy starts with its spa_interface, which points to the method table
    private SpaInterface* Interface => (SpaInterface*)proxy;

    private MetadataMethods* Methods => (MetadataMethods*)Interface->Funcs;

    /// <summary>Sets a property - a null <paramref name="value"/> removes it.</summary>
    public void SetProperty(uint subject, string key, string? type, string? value)
    {
        var keyNative = Marshal.StringToCoTaskMemUTF8(key);
        var typeNative = type is null ? IntPtr.Zero : Marshal.StringToCoTaskMemUTF8(type);
        var valueNative = value is null ? IntPtr.Zero : Marshal.StringToCoTaskMemUTF8(value);

        try
        {
            using var _ = connection.Lock();

            if (destroyed)
                return;

            var result = Methods->SetProperty(
                Interface->Data,
                subject,
                (byte*)keyNative,
                (byte*)typeNative,
                (byte*)valueNative
            );

            if (result < 0)
                Console.WriteLine($"Setting {key} of {subject} in metadata {GlobalId} failed: {result}");
        }
        finally
        {
            Marshal.FreeCoTaskMem(keyNative);
            Marshal.FreeCoTaskMem(typeNative);
            Marshal.FreeCoTaskMem(valueNative);
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
    private static int OnProperty(IntPtr data, uint subject, IntPtr key, IntPtr type, IntPtr value)
    {
        // An exception thrown into native code kills the process
        try
        {
            var target = (PipeWireMetadata)GCHandle.FromIntPtr(data).Target!;

            // Valid only during this callback - copy them
            var keyText = Marshal.PtrToStringUTF8(key);
            var typeText = Marshal.PtrToStringUTF8(type);
            var valueText = Marshal.PtrToStringUTF8(value);

            PipeWireConnection.RunOnMainThread(() =>
            {
                if (!target.destroyed)
                    target.PropertyChanged?.Invoke(subject, keyText, typeText, valueText);
            });
        }
        catch (Exception exception)
        {
            Console.WriteLine($"PipeWire metadata callback failed: {exception}");
        }

        return 0;
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
    private static partial void pw_proxy_destroy(IntPtr proxy);
}
