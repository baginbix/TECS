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
        public long exludeMask;
        public long includeMask;

        public QueryFilter With<T>() where T: struct
        {
            includeMask |= 1L << ComponentID<T>.Value;
            return this;
        }

        public QueryFilter Without<T>() where T: struct
        {
            exludeMask |= 1L << ComponentID<T>.Value;
            return this;
        }
    }
}