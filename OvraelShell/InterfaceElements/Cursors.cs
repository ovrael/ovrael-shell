namespace OvraelShell.InterfaceElements;

public static class Cursors
{
    public static readonly Gdk.Cursor Default = Gdk.Cursor.NewFromName("default", null)!;
    public static readonly Gdk.Cursor Pointer = Gdk.Cursor.NewFromName("pointer", null) ?? Default;
    public static readonly Gdk.Cursor Wait = Gdk.Cursor.NewFromName("wait", null) ?? Default;
    public static readonly Gdk.Cursor Text = Gdk.Cursor.NewFromName("text", null) ?? Default;
    public static readonly Gdk.Cursor Progress =
        Gdk.Cursor.NewFromName("progress", null) ?? Default;
    public static readonly Gdk.Cursor Crosshair =
        Gdk.Cursor.NewFromName("crosshair", null) ?? Default;
    public static readonly Gdk.Cursor Grab = Gdk.Cursor.NewFromName("grab", null) ?? Default;
    public static readonly Gdk.Cursor NotAllowed =
        Gdk.Cursor.NewFromName("not-allowed", null) ?? Default;
}
