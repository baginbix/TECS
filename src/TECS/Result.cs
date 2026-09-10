using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace TECS.Result;

public ref struct Result<T, E>
{
    private ref readonly T value;
    public ref readonly T Value => ref value;
    public bool IsSuccess { get; }
    public E Error { get; }

    private Result(ref T value, bool isSuccess, E error)
    {
        this.value = ref value;
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result<T, E> Success(ref T value) => new Result<T, E>(ref value, true, default!);

    public static Result<T, E> Failure(E error) =>
        new Result<T, E>(ref Unsafe.NullRef<T>(), false, error);

    public ref readonly T Unwrap()
    {
#if DEBUG
        if (!IsSuccess)
            throw new InvalidOperationException(
                $"Tried to unwrap a value that doesn't exist. {Error}"
            );
#endif
        return ref value;
    }
}

public ref struct ResultMut<T, E>
{
    private ref T value;
    public ref T Value => ref value;
    public bool IsSuccess { get; }
    public E Error { get; }

    private ResultMut(ref T value, bool isSuccess, E error)
    {
        this.value = ref value;
        IsSuccess = isSuccess;
        Error = error;
    }

    public static ResultMut<T, E> Success(ref T value) =>
        new ResultMut<T, E>(ref value, true, default);

    public static ResultMut<T, E> Failure(E error) =>
        new ResultMut<T, E>(ref Unsafe.NullRef<T>(), false, error);

    public ref T Unwrap()
    {
#if DEBUG
        if (!IsSuccess)
            throw new InvalidOperationException(
                $"Tried to unwrap a value that doesn't exist. {Error}"
            );
#endif
        return ref value;
    }
}
