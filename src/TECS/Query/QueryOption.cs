using System.Diagnostics;

public readonly ref struct QueryOption<T>
    where T : allows ref struct
{
    private readonly T _value;
    private readonly bool _isSome;
    public bool IsSome => _isSome;
    public bool IsNone => !IsSome;

    private QueryOption(T value, bool isSome)
    {
        this._value = value;
        _isSome = isSome;
    }

    public static implicit operator QueryOption<T>(T value) => Some(value);

    public static QueryOption<T> Some(T value) => new(value, true);

    public static QueryOption<T> None() => new(default!, false);

    public T Unwrap()
    {
        Debug.Assert(_isSome, $"You are trying to unwrap an empty value of type:{typeof(T)}");
        return _value;
    }
}
