namespace OvraelShell.Models;

public sealed class ReactiveProperty<T>
{
    private T _value;

    public T Value => _value;

    public event Action<T>? Changed;

    public ReactiveProperty(T initialValue = default!)
    {
        _value = initialValue;
    }

    public bool Set(T value)
    {
        if (EqualityComparer<T>.Default.Equals(_value, value))
            return false;

        _value = value;
        Changed?.Invoke(value);

        return true;
    }

    public static implicit operator T(ReactiveProperty<T> property) => property.Value;

    public override string ToString() => _value?.ToString() ?? string.Empty;
}
