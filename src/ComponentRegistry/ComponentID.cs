using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace TECS.Components
{
    public static class ComponentID<T> where T: struct
    {
        public static readonly int Value = ComponentRegistry.Next;
    }
}