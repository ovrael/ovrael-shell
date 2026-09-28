/// <summary>A NetworkManager D-Bus object. The path identifies it and never changes.</summary>
public interface IWithObjectPath
{
    public string ObjectPath { get; }
}
