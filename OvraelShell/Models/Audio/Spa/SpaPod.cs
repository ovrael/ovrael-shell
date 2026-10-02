using System.Runtime.InteropServices;

namespace OvraelShell.Models.Audio.Spa;

/// <summary>
/// Minimal reader and builder of SPA PODs - the binary format of PipeWire params.
/// Every pod is a header (body size, type) followed by the body, padded to 8 bytes.
/// Only the types needed for volume are handled.
/// </summary>
internal static class SpaPod
{
    // enum spa_type in spa/utils/type.h
    public const uint TypeBool = 2;
    public const uint TypeId = 3;
    public const uint TypeInt = 4;
    public const uint TypeFloat = 6;
    public const uint TypeArray = 13;
    public const uint TypeObject = 15;

    private const int HeaderSize = 8;

    #region Reading

    /// <summary>
    /// Properties of an Object pod by key, each value a whole pod.
    /// Null when <paramref name="pod"/> is not an object of <paramref name="objectType"/>.
    /// </summary>
    public static Dictionary<uint, byte[]>? ReadObject(ReadOnlySpan<byte> pod, uint objectType)
    {
        // Header, then the object type and id
        if (pod.Length < 16 || U32(pod, 4) != TypeObject || U32(pod, 8) != objectType)
            return null;

        var end = Math.Min(pod.Length, HeaderSize + (int)U32(pod, 0));
        var props = new Dictionary<uint, byte[]>();

        // Each property: key, flags, then the value pod
        var offset = 16;
        while (offset + 16 <= end)
        {
            var key = U32(pod, offset);
            var valueLength = HeaderSize + (int)U32(pod, offset + 8);

            if (offset + 8 + valueLength > end)
                break;

            props[key] = pod.Slice(offset + 8, valueLength).ToArray();
            offset += 8 + Pad(valueLength);
        }

        return props;
    }

    public static bool? ReadBool(byte[]? pod) =>
        pod is { Length: >= 12 } && U32(pod, 4) == TypeBool ? U32(pod, 8) != 0 : null;

    /// <summary>An Int or an Id.</summary>
    public static int? ReadInt(byte[]? pod) =>
        pod is { Length: >= 12 } && U32(pod, 4) is TypeInt or TypeId ? (int)U32(pod, 8) : null;

    public static float[]? ReadFloatArray(byte[]? pod)
    {
        // Header, then the header of the children (size, type) and the packed values
        if (pod is not { Length: >= 16 } || U32(pod, 4) != TypeArray)
            return null;

        if (U32(pod, 8) != sizeof(float) || U32(pod, 12) != TypeFloat)
            return null;

        var bodySize = Math.Min((int)U32(pod, 0), pod.Length - HeaderSize);
        var count = (bodySize - 8) / sizeof(float);

        return MemoryMarshal.Cast<byte, float>(pod.AsSpan(16, count * sizeof(float))).ToArray();
    }

    private static uint U32(ReadOnlySpan<byte> pod, int offset) =>
        MemoryMarshal.Read<uint>(pod[offset..]);

    #endregion

    #region Building

    public static byte[] Bool(bool value) => Primitive(TypeBool, value ? 1u : 0u);

    public static byte[] Int(int value) => Primitive(TypeInt, (uint)value);

    public static byte[] FloatArray(IReadOnlyList<float> values)
    {
        var bodySize = 8 + values.Count * sizeof(float);
        var pod = new byte[HeaderSize + Pad(bodySize)];

        Write(pod, 0, (uint)bodySize);
        Write(pod, 4, TypeArray);
        Write(pod, 8, sizeof(float));
        Write(pod, 12, TypeFloat);

        for (var i = 0; i < values.Count; i++)
            MemoryMarshal.Write(pod.AsSpan(16 + i * sizeof(float)), values[i]);

        return pod;
    }

    /// <summary>An object with <paramref name="props"/> - values are whole pods, already padded.</summary>
    public static byte[] Object(uint objectType, uint objectId, params (uint Key, byte[] Value)[] props)
    {
        var bodySize = 8 + props.Sum(prop => 8 + prop.Value.Length);
        var pod = new byte[HeaderSize + bodySize];

        Write(pod, 0, (uint)bodySize);
        Write(pod, 4, TypeObject);
        Write(pod, 8, objectType);
        Write(pod, 12, objectId);

        var offset = 16;
        foreach (var (key, value) in props)
        {
            Write(pod, offset, key);

            // Flags stay 0
            value.CopyTo(pod, offset + 8);
            offset += 8 + value.Length;
        }

        return pod;
    }

    private static byte[] Primitive(uint type, uint value)
    {
        var pod = new byte[HeaderSize + 8];

        Write(pod, 0, 4);
        Write(pod, 4, type);
        Write(pod, 8, value);

        return pod;
    }

    private static void Write(byte[] pod, int offset, uint value) =>
        MemoryMarshal.Write(pod.AsSpan(offset), value);

    #endregion

    private static int Pad(int size) => (size + 7) & ~7;
}
