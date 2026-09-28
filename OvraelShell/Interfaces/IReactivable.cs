namespace OvraelShell.Interfaces;

public interface IReactivable<T>
{
    public event Action<T>? OnChange;
}
