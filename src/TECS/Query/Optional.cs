using System.Diagnostics;
using System.Runtime.CompilerServices;

public ref struct Optional<T>
{
    private ref readonly T _value;
    private bool _isSome;

    private Optional(in T value, bool isSome)
    {
        _value = ref value;
        _isSome = isSome;
    }

    public static Optional<T> Some(ref readonly T value) => new(in value, true);

    public static Optional<T> None() => new(in Unsafe.NullRef<T>(), false);

    public bool IsSome => _isSome;
    public bool IsNone => !_isSome;

    public ref readonly T Unwrap()
    {
        Debug.Assert(_isSome, "Tried to unwrap a None Optional!");
        return ref _value;
    }
}
