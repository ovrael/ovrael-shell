namespace OvraelShell.Models;

public sealed class ReactiveProperty<T>
{
    private T _value;
    private readonly IEqualityComparer<T> _comparer;

    public T Value => _value;

    public event Action<T>? Changed;

    /// <param name="comparer">Decides when a new value is a change. Default equality if null.</param>
    public ReactiveProperty(T initialValue = default!, IEqualityComparer<T>? comparer = null)
    {
        _value = initialValue;
        _comparer = comparer ?? EqualityComparer<T>.Default;
    }

    public bool Set(T value)
    {
        if (_comparer.Equals(_value, value))
            return false;

        _value = value;
        Changed?.Invoke(value);

        return true;
    }

    public static implicit operator T(ReactiveProperty<T> property) => property.Value;

    public override string ToString() => _value?.ToString() ?? string.Empty;
}
