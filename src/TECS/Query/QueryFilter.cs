
using TECS.Components;

namespace TECS.Queries
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