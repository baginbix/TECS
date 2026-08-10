using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TECS.Resources;
public readonly ref struct Res<T> where T : IResource
{
    public readonly ref readonly T Value;
    internal Res(ref T value)
    {
        Value = ref value;
    }

}
