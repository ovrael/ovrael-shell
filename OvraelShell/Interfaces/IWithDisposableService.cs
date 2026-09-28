using OvraelShell.Services;

public interface IWithDisposableService<T>
    where T : IDisposable
{
    public T Service { get; }
    public void AddService(T audioService);

    /// <summary>Stops following the service - it outlives the widget, which it would keep alive otherwise.</summary>
    public void RemoveService();
}
