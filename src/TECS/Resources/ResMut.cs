using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TECS.Resources;

namespace TECS.Resources;
public ref struct ResMut<T> where T : IResource
{
    public ref T Value;
    internal ResMut(ref T value)
    {
        Value = ref value;
    }
}
