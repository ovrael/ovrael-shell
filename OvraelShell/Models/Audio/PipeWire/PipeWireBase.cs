namespace OvraelShell.Models.Audio.PipeWire;

/// <summary>
/// Common part of PipeWire registry objects. A GObject, so they can be put in a Gio.ListStore.
/// </summary>
[GObject.Subclass<GObject.Object>]
public partial class PipeWireBase : IEquatable<PipeWireBase>
{
    public uint GlobalId { get; private set; }
    public string ObjectSerial { get; private set; } = string.Empty;
    public string? ObjectPath { get; private set; }

    public string? Api { get; private set; }
    public string? Name { get; private set; }
    public string? Nick { get; private set; }
    public string? Description { get; private set; }
    public string? MediaClass { get; private set; }

    /// <summary>What to show in the UI - some streams have no description, only a name.</summary>
    public string DisplayName => Description ?? Name ?? $"#{GlobalId}";

    /// <summary>Fills the common props - called by Create of the derived classes.</summary>
    /// <param name="prefix">Props prefix of the object type - "device" or "node"</param>
    protected void Fill(PipeWireGlobal global, string prefix)
    {
        GlobalId = global.Id;
        ObjectSerial = global.Prop("object.serial") ?? string.Empty;
        ObjectPath = global.Prop("object.path");

        Api = global.Prop($"{prefix}.api");
        Name = global.Prop($"{prefix}.name");
        Nick = global.Prop($"{prefix}.nick");
        Description = global.Prop($"{prefix}.description");
        MediaClass = global.Prop("media.class");
    }

    #region Equality

    // Wrappers from GTK models (ListStore, ListItem) are compared by global id, not by reference

    public bool Equals(PipeWireBase? other) =>
        other is not null && GetType() == other.GetType() && GlobalId == other.GlobalId;

    public override bool Equals(object? obj) => Equals(obj as PipeWireBase);

    public override int GetHashCode() => GlobalId.GetHashCode();

    public static bool operator ==(PipeWireBase? left, PipeWireBase? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(PipeWireBase? left, PipeWireBase? right) => !(left == right);

    #endregion
}
