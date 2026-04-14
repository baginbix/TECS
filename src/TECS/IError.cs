using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace TECS
{
    /// <summary>
    /// Contains a reference that's allowed to be changed
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public ref struct OptionRef<T>
    {
        private readonly ref T value;

        public readonly bool IsSome;
        public bool IsNone => !IsSome;

        public OptionRef(ref T value)
        {
            this.value = ref value;
            IsSome = true;
        }

        public OptionRef()
        {
            value = ref Unsafe.NullRef<T>();
            IsSome = false;
        }

        public static OptionRef<T> None => new OptionRef<T>();

        public ref T Unwrap()
        {
            if (IsNone)
            {
                throw new InvalidOperationException("Tried to unwrap a None OptionRef!");
            }

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
            if (IsNone)
            {
                throw new InvalidOperationException("Tried to unwrap a None OptionRef!");
            }

            return ref value;
        }
    }
}