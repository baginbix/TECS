using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace TECS
{
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
}