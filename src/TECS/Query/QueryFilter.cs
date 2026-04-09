using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using TECS;
using TECS.Components;

namespace src.Query
{
    public struct QueryFilter
    {
        public Bitset exludeMask;
        public Bitset includeMask;

        public QueryFilter With<T>() where T: struct
        {
            includeMask.SetBit(ComponentID<T>.Value);
            return this;
        }

        public QueryFilter Without<T>() where T: struct
        {
            exludeMask.SetBit(ComponentID<T>.Value);
            return this;
        }
    }
}