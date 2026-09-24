using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace TECS.Result;

/// <summary>
/// Contains a reference that's allowed to be changed
/// </summary>
/// <typeparam name="T"></typeparam>
[Obsolete("Note to self: Change these to Option so i can remove OptionMut")]
public ref struct OptionMut<T>
{
    private readonly ref T value;
    private readonly bool _isSome;

    public bool IsSome => _isSome;
    public bool IsNone => !IsSome;

    public OptionMut(ref T value)
    {
        this.value = ref value;
        _isSome = true;
    }

    public OptionMut()
    {
        value = ref Unsafe.NullRef<T>();
        _isSome = false;
    }

    public static OptionMut<T> None() => new OptionMut<T>();

    public static OptionMut<T> Some(ref T value) => new OptionMut<T>(ref value);

    public ref T Unwrap()
    {
        Debug.Assert(
            _isSome,
            $"Tried to unwrap a value that doesn't have a value of type:{typeof(T)}"
        );
        return ref value;
    }
}

/// <summary>
/// Contains a readonly reference
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly ref struct Option<T>
{
    private readonly ref T _value;
    private readonly bool _isSome;
    public bool IsSome => _isSome;
    public bool IsNone => !IsSome;

    public Option(ref T value)
    {
        this._value = ref value;
        _isSome = true;
    }

    public Option()
    {
        _value = ref Unsafe.NullRef<T>();
        _isSome = false;
    }

    public static Option<T> Some(ref T value) => new Option<T>(ref value);

    public static Option<T> None() => new Option<T>();

    public T Unwrap()
    {
        Debug.Assert(
            _isSome,
            $"Tried to unwrap a value that doesn't have a value of type:{typeof(T)}"
        );
        return _value;
    }
}
