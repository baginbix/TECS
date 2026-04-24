
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
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

    public struct QueryFilterImproved(ECS ecs)
    {
        private int includeCount;
        private int excludeCount;

        private int[][] inc0,inc1,inc2,inc3;
        private int[][] exc0,exc1,exc2,exc3;

        public QueryFilterImproved With<T>() where T: struct
        {
           var pages = ecs.GetOrCreateSet<T>().GetSparseSet(); 
            
            if (includeCount == 0) inc0 = pages;
            else if (includeCount == 1) inc1 = pages;
            else if (includeCount == 2) inc2 = pages;
            else if (includeCount == 3) inc3 = pages;

            includeCount++;
            return this;
        }

        public QueryFilterImproved Without<T>() where T: struct
        {
            var pages = ecs.GetOrCreateSet<T>().GetSparseSet();
            if (excludeCount == 0) exc0 = pages;
            else if (excludeCount == 1) exc1 = pages;
            else if (excludeCount == 2) exc2 = pages;
            else if (excludeCount == 3) exc3 = pages;

            excludeCount++;
            return this;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int id)
        {
            int pageIndex = id >> SparseSet<int>.PAGE_SHIFT;
            int pageOffset = id & SparseSet<int>.PAGE_MASK;

            
            if (includeCount > 0 && inc0[pageIndex][pageOffset] == -1) return false;
            if (includeCount > 1 && inc1[pageIndex][pageOffset] == -1) return false;
            if (includeCount > 2 && inc2[pageIndex][pageOffset] == -1) return false;
            if (includeCount > 3 && inc3[pageIndex][pageOffset] == -1) return false;

            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasNot(int id)
        {
            int pageIndex = id >> SparseSet<int>.PAGE_SHIFT;
            int pageOffset = id & SparseSet<int>.PAGE_MASK;
            if (excludeCount > 0 && inc0[pageIndex][pageOffset] == -1) return false;
            if (excludeCount > 1 && inc1[pageIndex][pageOffset] == -1) return false;
            if (excludeCount > 2 && inc2[pageIndex][pageOffset] == -1) return false;
            if (excludeCount > 3 && inc3[pageIndex][pageOffset] == -1) return false;
            return true;
        }

        public bool HasInclud() => includeCount> 0;
        public bool HasExclude() =>excludeCount > 0;
    }
}