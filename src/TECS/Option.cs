using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace TECS.Result;

/// <summary>
/// Contains a reference that's allowed to be changed
/// </summary>
/// <typeparam name="T"></typeparam>
public ref struct OptionMut<T>
{
    private readonly ref T value;

    public readonly bool IsSome;
    public bool IsNone => !IsSome;

    public OptionMut(ref T value)
    {
        this.value = ref value;
        IsSome = true;
    }

    public OptionMut()
    {
        value = ref Unsafe.NullRef<T>();
        IsSome = false;
    }

    public static OptionMut<T> None => new OptionMut<T>();

    public ref T Unwrap()
    {
#if DEBUG
        if (IsNone)
        {
            throw new InvalidOperationException("Tried to unwrap a None OptionRef!");
        }
#endif
        return ref value;
    }
}

/// <summary>
/// Contains a readonly reference
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly ref struct Option<T>
{
    private readonly ref T value;

    public readonly bool IsSome;
    public bool IsNone => !IsSome;

    public Option(ref T value)
    {
        this.value = ref value;
        IsSome = true;
    }

    public Option()
    {
        value = ref Unsafe.NullRef<T>();
        IsSome = false;
    }

    public static Option<T> None => new Option<T>();

    public readonly ref T Unwrap()
    {
#if DEBUG
        if (IsNone)
        {
            throw new InvalidOperationException("Tried to unwrap a None OptionRef!");
        }
#endif
        return ref value;
    }
}
