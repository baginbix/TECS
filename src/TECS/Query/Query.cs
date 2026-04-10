using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Tar;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using src.Query;

namespace TECS;
public delegate void QueryFunc<T>(ref T comp);
public delegate void QueryFuncEntity<T>(Entity entity, ref T comp);
public delegate void QueryFunc<T,E>(ref T comp1, ref E comp2);
public delegate void QueryFuncEntity<T,E>(Entity entity, ref T comp1, ref E comp2);
public delegate void QueryFunc<T,E,K>(ref T comp1, ref E comp2, ref K comp3);
public delegate void QueryFuncEntity<T,E,K>(Entity entity, ref T comp1, ref E comp2, ref K comp3);


public interface IQueryAction<T> where T: struct
{
    void Execute(ref T comp);
}
public interface IQueryAction<T, E> where T: struct where E: struct
{
    void Execute(ref T comp1, ref E comp2);
}

public interface IQueryAction<T, E, K> where T: struct where E: struct where K: struct
{
    void Execute(ref T comp1, ref E comp2, ref K comp3);
}

public ref struct ComponentItem<T> where T: struct
{
    private readonly ref T component;
    private readonly ref ulong tick;
    private readonly ulong globalTick;
    public ComponentItem(ref T component, ref ulong tick, ulong globalTick)
    {
        this.component = ref component;
        this.tick = ref tick;
        this.globalTick = globalTick;
    }

    public ref readonly T RO => ref component;

    public ref T RW
    {
        get
        {
            tick = globalTick;
            return ref component;
        }
    }

}
public ref struct Query<T> where T : struct
{
    readonly Span<T> dense;
    readonly Span<Entity> entities;
    readonly Span<Bitset> entityMasks;
    QueryFilter queryFilter;
    readonly ulong lastSystemTick;
    readonly ulong lastGlobalTick;
    SparseSet<T> sparseSet;
    bool changed = false;
    public Query(SparseSet<T> sparseSet, Span<Bitset> entityMasks, ulong lastSystemTick, ulong lastGlobalTick)
    {
        this.dense = CollectionsMarshal.AsSpan(sparseSet.GetDense());
        entities = CollectionsMarshal.AsSpan(sparseSet.GetEntities());
        this.entityMasks = entityMasks;
        queryFilter = new QueryFilter();
        this.lastSystemTick = lastSystemTick;
        this.lastGlobalTick = lastGlobalTick;
        this.sparseSet = sparseSet;
    }

    public Query<T> With<Component>()
    where Component: struct
    {
        queryFilter.With<Component>();
        return this;
    }
    public Query<T> Without<Component>()
    where Component: struct
    {
        queryFilter.Without<Component>();
        return this;
    }

    public Query<T> Changed()
    {
        changed = true;
        return this;
    }

    public void ForEach(QueryFunc<T> func)
    {
        for(int i = 0; i < dense.Length; i++)
        {
            ref Bitset entityMask = ref entityMasks[entities[i].Id];
            if(entityMask.Intersects(in queryFilter.exludeMask)) continue;
            if(!entityMask.ContainsAll(in queryFilter.includeMask)) continue;
            func(ref dense[i]);
        }
    }
    
    public void ForEach(QueryFuncEntity<T> func)
    {
        for(int i = 0; i < dense.Length; i++)
        {
            ref Bitset entityMask = ref entityMasks[entities[i].Id];
            if(entityMask.Intersects(in queryFilter.exludeMask)) continue;
            if(!entityMask.ContainsAll(in queryFilter.includeMask)) continue;
            func(entities[i],ref dense[i]);
        }
    }

    public void ForEach<IAction>(IAction action) where IAction : struct, IQueryAction<T>{
        for(int i = 0; i < dense.Length; i++)
        {
            ref Bitset entityMask = ref entityMasks[entities[i].Id];
            if(entityMask.Intersects(in queryFilter.exludeMask)) continue;
            if(!entityMask.ContainsAll(in queryFilter.includeMask)) continue;
            action.Execute(ref dense[i]);
        }
    }

    public QueryEnumerator GetEnumerator() => new QueryEnumerator(sparseSet, entityMasks, queryFilter, lastSystemTick, lastGlobalTick, changed);
    

    public unsafe ref struct QueryEnumerator
    {
        private ref T dense;
        private readonly int denseLength;
        private int index;

        private ref Entity entities;
        private ref Bitset entityMasks;
        private readonly ref Bitset includeFilter;
        private readonly ref Bitset excludeFilter;
        private ref ulong ticks;
        private readonly ulong lastGlobalTick;
        private readonly ulong lastSystemTick;
        private readonly bool changed;

        private readonly bool hasInlcude;
        private readonly bool hasExclude;

        public QueryEnumerator(SparseSet<T> sparseSet, Span<Bitset> entityMasks, QueryFilter queryFilter, ulong lastSystemTick, ulong lastGlobalTick, bool changed)
        {
            this.dense = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(sparseSet.GetDense()));
            this.entities = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(sparseSet.GetEntities()));
            this.entityMasks = ref MemoryMarshal.GetReference(entityMasks);
            this.includeFilter = ref queryFilter.includeMask;
            this.excludeFilter = ref queryFilter.exludeMask;
            hasExclude = !excludeFilter.IsEmpty();
            hasInlcude = !includeFilter.IsEmpty();
            index = -1;

            this.lastSystemTick = lastSystemTick;
            this.lastGlobalTick = lastGlobalTick;

            this.changed = changed;
            denseLength = sparseSet.Size;

            this.ticks = ref MemoryMarshal.GetReference(sparseSet.GetLastTicks());
        }

        public bool MoveNext()
        {
            if (changed)
            {
                // If the query is filtered to only include changed components, we can skip entities until we find one that has been changed since the last time the system ran
                while(++index < denseLength)
                {
                    int entityId = Unsafe.Add(ref entities, index).Id;
                    ref Bitset entityMask = ref Unsafe.Add(ref entityMasks, entityId);
                    if(entityMask.Intersects(in excludeFilter)) continue;
                    if(!entityMask.ContainsAll(in includeFilter)) continue;
                    if(Unsafe.Add(ref ticks, index) <= lastSystemTick) continue; // Component hasn't been changed since the last time the system ran
                    return true;
                }

                return false;
            }
            while(++index < denseLength)
            {
                //int entityId = Unsafe.As<Entity,int>(ref Unsafe.Add(ref entities, index));
                int entityId = Unsafe.Add(ref entities, index).Id;
                ref Bitset entityMask = ref Unsafe.Add(ref entityMasks, entityId);
                if(hasExclude && entityMask.Intersects(in excludeFilter)) continue;
                if(hasInlcude && !entityMask.ContainsAll(in includeFilter)) continue;
                
                return true;
            }

            return false;
        }

        public ComponentItem<T> Current{
            get{
                return new ComponentItem<T>(
                    ref Unsafe.Add(ref dense, index), 
                    ref Unsafe.Add(ref ticks, index), 
                    lastGlobalTick);
            }
        }
    }

    public Span<T> GetPacked()
    {
        return dense;
    }
}


public ref struct Query<T, E, K> 
where T: struct 
where  E: struct
where K: struct
{
    [Flags]
    public enum ChangedSet : byte
    {
        None = 0,
        T = 1 << 0,
        E = 1 << 1,
        K = 1 << 2
    }
    
    SparseSet<E> sparseE;
    SparseSet<T> sparseT;
    SparseSet<K> sparseK;

    QueryFilter queryFilter;

    Span<Bitset> entitiesMask;

    readonly ulong lastSystemTick;
    readonly ulong lastGlobalTick;
    private ChangedSet changedMask;
    
    public Query(SparseSet<T> s1, SparseSet<E> s2, SparseSet<K> s3, Span<Bitset> entitiesMask, ulong lastSystemTick, ulong lastGlobalTick)
    {
        sparseE = s2;
        sparseT = s1;
        sparseK = s3;
        queryFilter = new QueryFilter();
        this.entitiesMask = entitiesMask;
        this.lastSystemTick = lastSystemTick;
        this.lastGlobalTick = lastGlobalTick;

        changedMask = ChangedSet.None;
    }

    public Query<T,E,K> With<Component>()
    where Component: struct
    {
        queryFilter.With<Component>();
        return this;
    }

    public Query<T,E,K> Without<Component>()
    where Component: struct
    {
        queryFilter.Without<Component>();
        return this;
    }

    public Query<T,E,K> Changed<TComponent>()
    where TComponent: struct
    {
        if (typeof(TComponent) == typeof(T)) changedMask |= ChangedSet.T;
        if (typeof(TComponent) == typeof(E)) changedMask |= ChangedSet.E;
        if (typeof(TComponent) == typeof(K)) changedMask |= ChangedSet.K;
        #if DEBUG
        else
        {
            throw new InvalidOperationException($"Type {typeof(TComponent)} is not part of the query");
        }
        #endif

        return this;
    }

    public void ForEach(QueryFunc<T,E,K> func)
    {
        SparseSet<T> s1 = sparseT;
        SparseSet<E> s2 = sparseE;
        SparseSet<K> s3 = sparseK;
        var denseT = CollectionsMarshal.AsSpan(s1.GetDense());
        var denseE = CollectionsMarshal.AsSpan(s2.GetDense());
        var denseK = CollectionsMarshal.AsSpan(s3.GetDense());



        if(sparseT.Size<sparseE.Size && sparseT.Size < sparseK.Size){
            var entities = CollectionsMarshal.AsSpan(s1.GetEntities());
            var entitiesE = s2.GetSparseSet().AsSpan();
            var entitiesK = s3.GetSparseSet().AsSpan();
            for(int i = 0; i < denseT.Length; i++)
            {
                int entityId = entities[i].Id;
                var pageIndex = entityId >> SparseSet<T>.PAGE_SHIFT;    
                if(pageIndex >= entitiesE.Length) continue;
                if(pageIndex >= entitiesK.Length) continue;

                var pageE = entitiesE[pageIndex];
                var pageK = entitiesK[pageIndex];
                if(pageE == null || pageK == null) continue;


                var pageOffset = entityId & SparseSet<T>.PAGE_MASK; 
                if(pageOffset >=  entitiesE[pageIndex].Length) continue;
                if(pageOffset >=  entitiesK[pageIndex].Length) continue;
                Bitset entityMask = entitiesMask[entityId];
                if(entityMask.Intersects(in queryFilter.exludeMask)) continue;
                if(!entityMask.ContainsAll(in queryFilter.includeMask)) continue;
                int indexE = pageE[pageOffset];
                int indexK = pageK[pageOffset];
                if(indexE != -1 && indexK != -1){
                    func(ref denseT[i], ref denseE[indexE], ref denseK[indexK]);
                }   
            }
        }
        else if(sparseE.Size < sparseK.Size)
        {
            var entities = CollectionsMarshal.AsSpan(s2.GetEntities());
            var entitiesT = s1.GetSparseSet().AsSpan();
            var entitiesK = s3.GetSparseSet().AsSpan();
            for(int i = 0; i < denseE.Length; i++)
            {
                int entityId = entities[i].Id;
                var pageIndex = entityId >> SparseSet<E>.PAGE_SHIFT;
                if(pageIndex >= entitiesT.Length) continue;
                if(pageIndex >= entitiesK.Length) continue;

                var pageT = entitiesT[pageIndex];
                var pageK = entitiesK[pageIndex];
                if(pageT == null || pageK == null) continue;

                var pageOffset = entityId & SparseSet<E>.PAGE_MASK;

                Bitset entityMask = entitiesMask[entityId];
                if(entityMask.Intersects(in queryFilter.exludeMask)) continue;
                if(!entityMask.ContainsAll(in queryFilter.includeMask)) continue;
                int indexT = pageT[pageOffset];
                int indexK = pageK[pageOffset];
                if(indexT != -1 && indexK != -1){
                    func(ref denseT[indexT], ref denseE[i], ref denseK[indexK]);
                }   
            }
        }
        else
        {
            var entities = CollectionsMarshal.AsSpan(s3.GetEntities());
            var entitiesT = s1.GetSparseSet().AsSpan();
            var entitiesE = s2.GetSparseSet().AsSpan();
            for(int i = 0; i < denseK.Length; i++)
            {
                int entityId = entities[i].Id;
                var pageIndex = entityId >> SparseSet<K>.PAGE_SHIFT;
                if(pageIndex >= entitiesT.Length) continue;
                if(pageIndex >= entitiesE.Length) continue;
                var pageT = entitiesT[pageIndex];
                var pageE = entitiesE[pageIndex];
                if(pageT == null || pageE == null) continue;
                var pageOffset = entityId & SparseSet<K>.PAGE_MASK;

                Bitset entityMask = entitiesMask[entityId];
                if(entityMask.Intersects(in queryFilter.exludeMask)) continue;
                if(!entityMask.ContainsAll(in queryFilter.includeMask)) continue;

                int indexT = pageT[pageOffset];
                int indexE = pageE[pageOffset];

                if(indexT != -1 && indexE != -1){
                    func(ref denseT[indexT], ref denseE[indexE], ref denseK[i]);
                }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="func"></param>
    public void ForEach(QueryFuncEntity<T,E,K> func)
    {
        SparseSet<T> s1 = sparseT;
        SparseSet<E> s2 = sparseE;
        SparseSet<K> s3 = sparseK;
        var denseT = CollectionsMarshal.AsSpan(s1.GetDense());
        var denseE = CollectionsMarshal.AsSpan(s2.GetDense());
        var denseK = CollectionsMarshal.AsSpan(s3.GetDense());
        if(sparseT.Size<sparseE.Size && sparseT.Size < sparseK.Size){
            var entities = CollectionsMarshal.AsSpan(s1.GetEntities());
            var entitiesE = s2.GetSparseSet().AsSpan();
            var entitiesK = s3.GetSparseSet().AsSpan();
            for(int i = 0; i < denseT.Length; i++)
            {
                int entityId = entities[i].Id;
                var pageIndex = entityId >> SparseSet<T>.PAGE_SHIFT;
                if(pageIndex >= entitiesE.Length) continue;
                if(pageIndex >= entitiesK.Length) continue;
                var pageE = entitiesE[pageIndex];
                var pageK = entitiesK[pageIndex];
                if(pageE == null || pageK == null) continue;
                var pageOffset = entityId & SparseSet<T>.PAGE_MASK;
                Bitset entityMask = entitiesMask[entityId];
                if(entityMask.Intersects(in queryFilter.exludeMask)) continue;
                if(!entityMask.ContainsAll(in queryFilter.includeMask)) continue;
                int indexE = pageE[pageOffset];
                int indexK = pageK[pageOffset];
                if(indexE != -1 && indexK != -1){
                    func(entities[i], ref denseT[i], ref denseE[indexE], ref denseK[indexK]);
                }   
            }
        }
        else if(sparseE.Size < sparseK.Size)
        {
            var entities = CollectionsMarshal.AsSpan(s2.GetEntities());
            var entitiesT = s1.GetSparseSet().AsSpan();
            var entitiesK = s3.GetSparseSet().AsSpan();
            for(int i = 0; i < denseE.Length; i++)
            {
                int entityId = entities[i].Id;
                var pageIndex = entityId >> SparseSet<E>.PAGE_SHIFT;
                if(pageIndex >= entitiesT.Length) continue;
                if(pageIndex >= entitiesK.Length) continue;
                
                var pageT = entitiesT[pageIndex];
                var pageK = entitiesK[pageIndex];
                if(pageT == null || pageK == null) continue;
                
                var pageOffset = entityId & SparseSet<E>.PAGE_MASK;
                
                Bitset entityMask = entitiesMask[entityId];
                if(entityMask.Intersects(in queryFilter.exludeMask)) continue;
                if(!entityMask.ContainsAll(in queryFilter.includeMask)) continue;
                int indexT = pageT[pageOffset];
                int indexK = pageK[pageOffset];
                if(indexT != -1 && indexK != -1){
                    func(entities[i], ref denseT[indexT], ref denseE[i], ref denseK[indexK]);
                }   
            }
        }
        else
        {
            var entities = CollectionsMarshal.AsSpan(s3.GetEntities());
            var entitiesT = s1.GetSparseSet().AsSpan();
            var entitiesE = s2.GetSparseSet().AsSpan();
            for(int i = 0; i < denseK.Length; i++)
            {
                int entityId = entities[i].Id;
                var pageIndex = entityId >> SparseSet<K>.PAGE_SHIFT;
                if(pageIndex >= entitiesT.Length) continue;
                if(pageIndex >= entitiesE.Length) continue;
                var pageT = entitiesT[pageIndex];
                var pageE = entitiesE[pageIndex];
                if(pageT == null || pageE == null) continue;
                var pageOffset = entityId & SparseSet<K>.PAGE_MASK;
                Bitset entityMask = entitiesMask[entityId];
                if(entityMask.Intersects(in queryFilter.exludeMask)) continue;
                if(!entityMask.ContainsAll(in queryFilter.includeMask)) continue;

                int indexT = pageT[pageOffset];
                int indexE = pageE[pageOffset];

                if(indexT != -1 && indexE != -1){
                    func(entities[i], ref denseT[indexT], ref denseE[indexE], ref denseK[i]);
                }
            }
        }
    }

    public void ForEach<IAction>(ref IAction action) 
    where IAction :struct, IQueryAction<T,E,K>
    {
        SparseSet<T> s1 = sparseT;
        SparseSet<E> s2 = sparseE;
        SparseSet<K> s3 = sparseK;
        var denseT = CollectionsMarshal.AsSpan(s1.GetDense());
        var denseE = CollectionsMarshal.AsSpan(s2.GetDense());
        var denseK = CollectionsMarshal.AsSpan(s3.GetDense());
        if(sparseT.Size<sparseE.Size && sparseT.Size < sparseK.Size){
            var entities = CollectionsMarshal.AsSpan(s1.GetEntities());
            var entitiesE = s2.GetSparseSet().AsSpan();
            var entitiesK = s3.GetSparseSet().AsSpan();
            for(int i = 0; i < denseT.Length; i++)
            {
                int entityId = entities[i].Id;
                var pageIndex = entityId >> SparseSet<T>.PAGE_SHIFT;
                if(pageIndex >= entitiesE.Length) continue;
                if(pageIndex >= entitiesK.Length) continue;
                var pageE = entitiesE[pageIndex];
                var pageK = entitiesK[pageIndex];
                if(pageE == null || pageK == null) continue;
                var pageOffset = entityId & SparseSet<T>.PAGE_MASK;
                Bitset entityMask = entitiesMask[entityId];
                if(entityMask.Intersects(in queryFilter.exludeMask)) continue;
                if(!entityMask.ContainsAll(in queryFilter.includeMask)) continue;

                int indexE = pageE[pageOffset];
                int indexK = pageK[pageOffset];
                if(indexE != -1 && indexK != -1){
                    action.Execute(ref denseT[i], ref denseE[indexE], ref denseK[indexK]);
                }   
            }
        }
        else if(sparseE.Size < sparseK.Size)
        {
            var entities = CollectionsMarshal.AsSpan(s2.GetEntities());
            var entitiesT = s1.GetSparseSet().AsSpan();
            var entitiesK = s3.GetSparseSet().AsSpan();
            for(int i = 0; i < denseE.Length; i++)
            {
                int entityId = entities[i].Id;
                var pageIndex = entityId >> SparseSet<E>.PAGE_SHIFT;
                if(pageIndex >= entitiesT.Length) continue;
                if(pageIndex >= entitiesK.Length) continue;
                var pageT = entitiesT[pageIndex];
                var pageK = entitiesK[pageIndex];
                if(pageT == null || pageK == null) continue;
                var pageOffset = entityId & SparseSet<E>.PAGE_MASK;
                Bitset entityMask = entitiesMask[entityId];
                if(entityMask.Intersects(in queryFilter.exludeMask)) continue;
                if(!entityMask.ContainsAll(in queryFilter.includeMask)) continue;
                int indexT = pageT[pageOffset];
                int indexK = pageK[pageOffset];
                if(indexT != -1 && indexK != -1){
                    action.Execute(ref denseT[indexT], ref denseE[i], ref denseK[indexK]);
                }   
            }
        }
        else
        {
            var entities = CollectionsMarshal.AsSpan(s3.GetEntities());
            var entitiesT = s1.GetSparseSet().AsSpan();
            var entitiesE = s2.GetSparseSet().AsSpan();
            for(int i = 0; i < denseK.Length; i++)
            {
                int entityId = entities[i].Id;
                var pageIndex = entityId >> SparseSet<K>.PAGE_SHIFT;
                if(pageIndex >= entitiesT.Length) continue;
                if(pageIndex >= entitiesE.Length) continue;
                var pageT = entitiesT[pageIndex];
                var pageE = entitiesE[pageIndex];
                if(pageT == null || pageE == null) continue;
                var pageOffset = entityId & SparseSet<K>.PAGE_MASK;
                Bitset entityMask = entitiesMask[entityId];
                if(entityMask.Intersects(in queryFilter.exludeMask)) continue;
                if(!entityMask.ContainsAll(in queryFilter.includeMask)) continue;
                int indexT = pageT[pageOffset];
                int indexE = pageE[pageOffset];
                if(indexT != -1 && indexE != -1){
                    action.Execute(ref denseT[indexT], ref denseE[indexE], ref denseK[i]);
                }
            }
        }
    }

    public QueryEnumerator GetEnumerator() => new QueryEnumerator(sparseE, sparseT, sparseK, entitiesMask, queryFilter, lastSystemTick, lastGlobalTick, changedMask);

    public readonly ref struct QueryItem
    {
        private readonly ref ulong tick1;
        private readonly ref ulong tick2;
        private readonly ref ulong tick3;
        private readonly ulong globalTick;
        private readonly ref T component1;
        private readonly ref E component2;
        private readonly ref K component3;
        private readonly Entity entity;

        public QueryItem(ref T comp1, ref E comp2, ref K comp3, ref ulong tick1, ref ulong tick2, ref ulong tick3, ulong globalTick)
        {
            component1 = ref comp1;
            component2 = ref comp2;
            component3 = ref comp3;
            this.tick1 = ref tick1;
            this.tick2 = ref tick2;
            this.tick3 = ref tick3;
            this.globalTick = globalTick;
        }

        public void Deconstruct(out ComponentItem<T> comp1, out ComponentItem<E> comp2, out ComponentItem<K> comp3)
        {
            comp1 = new ComponentItem<T>(ref component1, ref tick1, globalTick);
            comp2 = new ComponentItem<E>(ref component2, ref tick2, globalTick);
            comp3 = new ComponentItem<K>(ref component3, ref tick3, globalTick);
        }
        public void Deconstruct(out Entity entity, out ComponentItem<T> comp1, out ComponentItem<E> comp2, out ComponentItem<K> comp3)
        {
            entity = this.entity;
            comp1 = new ComponentItem<T>(ref component1, ref tick1, globalTick);
            comp2 = new ComponentItem<E>(ref component2, ref tick2, globalTick);
            comp3 = new ComponentItem<K>(ref component3, ref tick3, globalTick);
        }

        public Entity Entity => entity;

        public ref readonly TRead RO<TRead>() where TRead : struct
        {
            if(typeof(TRead) == typeof(T))
            {
                return ref Unsafe.As<T, TRead>(ref component1);
            }
            else if(typeof(TRead) == typeof(E))
            {
                return ref Unsafe.As<E, TRead>(ref component2);
            }
            else if(typeof(TRead) == typeof(K))
            {
                return ref Unsafe.As<K, TRead>(ref component3);
            }

            throw new InvalidOperationException($"Type {typeof(TRead)} is not part of the query");

        }
        public ref TWrite RW<TWrite>() where TWrite : struct
        {
            if(typeof(TWrite) == typeof(T))
            {
                tick1 = globalTick;
                return ref Unsafe.As<T, TWrite>(ref component1);
            }
            else if(typeof(TWrite) == typeof(E))
            {
                tick2 = globalTick;
                return ref Unsafe.As<E, TWrite>(ref component2);
            }
            else if(typeof(TWrite) == typeof(K))
            {
                tick3 = globalTick;
                return ref Unsafe.As<K, TWrite>(ref component3);
            }

            throw new InvalidOperationException($"Type {typeof(TWrite)} is not part of the query");

        }
    }


    [StructLayout(LayoutKind.Auto)]
    public unsafe ref struct QueryEnumerator
    {
        enum SmallestSet { T, E, K }
        
        // 1. Ditch the Spans! Store direct references. (8 bytes each instead of 16)
        private ref E denseE_Ref;
        private ref T denseT_Ref;
        private ref K denseK_Ref;

        // We only need the entities of the SMALLEST set to drive the loop!
        private ref Entity denseEntities_Ref; 

        private ref int[] sparseEntitiesE_Ref;
        private ref int[] sparseEntitiesT_Ref;
        private ref int[] sparseEntitiesK_Ref;

        private ref Bitset entitiesMask_Ref;

        private readonly ref Bitset includeMask;
        private readonly ref Bitset excludeMask;

        private readonly ulong lastSystemTick;
        private readonly ulong lastGlobalTick;
        
        private ref ulong ticksT_Ref;
        private ref ulong ticksE_Ref;
        private ref ulong ticksK_Ref;


        private int index;
        private int idxT, idxE, idxK;
        private readonly int denseLength;



        private readonly SmallestSet smallestSet;
        private readonly bool checkChangedT;
        private readonly bool checkChangedE;
        private readonly bool checkChangedK;

        private readonly bool hasIncludeFilter;
        private readonly bool hasExcludeFilter;
        private int page = -1;

        public QueryEnumerator(SparseSet<E> sparseE, SparseSet<T> sparseT, SparseSet<K> sparseK, Span<Bitset> entitiesMask, QueryFilter queryFilter, ulong lastSystemTick, ulong lastGlobalTick, ChangedSet changedMask)
        {
            // Extract raw memory references to completely bypass bounds checking later
            denseE_Ref = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(sparseE.GetDense()));
            denseT_Ref = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(sparseT.GetDense()));
            denseK_Ref = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(sparseK.GetDense()));

            sparseEntitiesE_Ref = ref MemoryMarshal.GetReference(sparseE.GetSparseSet()); // We need to be able to index into the sparse sets by entity ID, so we need to convert them to arrays
            sparseEntitiesT_Ref = ref MemoryMarshal.GetReference(sparseT.GetSparseSet());
            sparseEntitiesK_Ref = ref MemoryMarshal.GetReference(sparseK.GetSparseSet());

            ticksT_Ref = ref MemoryMarshal.GetReference(sparseT.GetLastTicks());
            ticksE_Ref = ref MemoryMarshal.GetReference(sparseE.GetLastTicks());
            ticksK_Ref = ref MemoryMarshal.GetReference(sparseK.GetLastTicks());

            entitiesMask_Ref = ref MemoryMarshal.GetReference(entitiesMask);

            this.lastSystemTick = lastSystemTick;
            this.lastGlobalTick = lastGlobalTick;
            includeMask = ref queryFilter.includeMask;
            excludeMask = ref queryFilter.exludeMask;
            
            hasExcludeFilter = !queryFilter.exludeMask.IsEmpty();
            hasIncludeFilter = !queryFilter.includeMask.IsEmpty();

            this.checkChangedE = changedMask.HasFlag(ChangedSet.E);
            this.checkChangedT = changedMask.HasFlag(ChangedSet.T);
            this.checkChangedK = changedMask.HasFlag(ChangedSet.K);
            this.index = -1;

            // Determine which set drives the loop and ONLY store its entities
            if(sparseT.Size < sparseE.Size && sparseT.Size < sparseK.Size)
            {
                smallestSet = SmallestSet.T;
                denseEntities_Ref = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(sparseT.GetEntities()));
                denseLength = sparseT.Size;
            }
            else if(sparseE.Size < sparseK.Size)
            {
                smallestSet = SmallestSet.E;
                denseEntities_Ref = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(sparseE.GetEntities()));
                denseLength = sparseE.Size;
            }
            else
            {
                smallestSet = SmallestSet.K;
                denseEntities_Ref = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(sparseK.GetEntities()));
                denseLength = sparseK.Size;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            int[] pageE = null, pageT = null, pageK = null;
            page = -1; // Reset cached page index at the start of each MoveNext call
            if(smallestSet == SmallestSet.T)
            {
                while(++index < denseLength)
                {
                    // Unsafe.Add completely bypasses bounds checking
                    int entityId = Unsafe.Add(ref denseEntities_Ref, index).Id;
                    
                    ref Bitset entityMask = ref Unsafe.Add(ref entitiesMask_Ref, entityId);
                    if(hasExcludeFilter && entityMask.Intersects(in excludeMask)) continue;
                    if(hasIncludeFilter && !entityMask.ContainsAll(in includeMask)) continue;
                    
                    int pageIndex = entityId >> SparseSet<E>.PAGE_SHIFT;

                    if(pageIndex >= sparseEntitiesE_Ref.Length) continue;
                    if(pageIndex >= sparseEntitiesK_Ref.Length) continue;
                    if(pageIndex != page) // Cache the current page of the sparse sets to minimize redundant array accesses. This is a huge performance boost when components are clustered together in memory
                    {
                        pageE = Unsafe.Add(ref sparseEntitiesE_Ref, pageIndex);
                        pageK = Unsafe.Add(ref sparseEntitiesK_Ref, pageIndex);
                        page = pageIndex;
                    }
                    if(pageE == null || pageK == null) continue;
                    int pageOffset = entityId & SparseSet<E>.PAGE_MASK;
                    ref int pageDataRefE = ref MemoryMarshal.GetArrayDataReference(pageE);
                    ref int pageDataRefK = ref MemoryMarshal.GetArrayDataReference(pageK);
                    idxE = Unsafe.Add(ref pageDataRefE, pageOffset);
                    idxK = Unsafe.Add(ref pageDataRefK, pageOffset);
                    if((idxE | idxK) < 0) continue;  

                    if(checkChangedT && Unsafe.Add(ref ticksT_Ref, index) <= lastSystemTick) continue;
                    if(checkChangedE && Unsafe.Add(ref ticksE_Ref, idxE) <= lastSystemTick) continue;
                    if(checkChangedK && Unsafe.Add(ref ticksK_Ref, idxK) <= lastSystemTick) continue;

                    idxT = index;
                    return true;
                }
            }
            else if(smallestSet == SmallestSet.E)
            {
                while(++index < denseLength)
                {
                    int entityId = Unsafe.Add(ref denseEntities_Ref, index).Id;
                    
                    
                    ref Bitset entityMask = ref Unsafe.Add(ref entitiesMask_Ref, entityId);
                    if(hasExcludeFilter && entityMask.Intersects(in excludeMask)) continue;
                    if(hasIncludeFilter && !entityMask.ContainsAll(in includeMask)) continue;

                    int pageIndex = entityId >> SparseSet<T>.PAGE_SHIFT;
                    if(pageIndex >= sparseEntitiesT_Ref.Length) continue;
                    if(pageIndex >= sparseEntitiesK_Ref.Length) continue;
                    int[] pageT = Unsafe.Add(ref sparseEntitiesT_Ref, pageIndex);
                    int[] pageK = Unsafe.Add(ref sparseEntitiesK_Ref, pageIndex);
                    int pageOffset = entityId & SparseSet<T>.PAGE_MASK;
                    ref int pageDataRefT = ref MemoryMarshal.GetArrayDataReference(pageT);
                    ref int pageDataRefK = ref MemoryMarshal.GetArrayDataReference(pageK);
                    idxT = Unsafe.Add(ref pageDataRefT, pageOffset);
                    idxK = Unsafe.Add(ref pageDataRefK, pageOffset);
                    if((idxT | idxK) < 0) continue;  

                    if(checkChangedE && Unsafe.Add(ref ticksE_Ref, index) <= lastSystemTick) continue;
                    if(checkChangedT && Unsafe.Add(ref ticksT_Ref, idxT) <= lastSystemTick) continue;
                    if(checkChangedK && Unsafe.Add(ref ticksK_Ref, idxK) <= lastSystemTick) continue;

                    idxE = index;                   
                    return true;
                }
            }
            else // Smallest is K
            {
                while(++index < denseLength)
                {
                    int entityId = Unsafe.Add(ref denseEntities_Ref, index).Id;
                    
                    
                    ref Bitset entityMask = ref Unsafe.Add(ref entitiesMask_Ref, entityId);
                    if(hasExcludeFilter && entityMask.Intersects(in excludeMask)) continue;
                    if(hasIncludeFilter && !entityMask.ContainsAll(in includeMask)) continue;

                    int pageIndex = entityId >> SparseSet<T>.PAGE_SHIFT;
                    if(pageIndex >= sparseEntitiesT_Ref.Length) continue;
                    if(pageIndex >= sparseEntitiesE_Ref.Length) continue;
                    int[] pageT = Unsafe.Add(ref sparseEntitiesT_Ref, pageIndex);
                    int[] pageE = Unsafe.Add(ref sparseEntitiesE_Ref, pageIndex);
                    if(pageT == null || pageE == null) continue;
                    int pageOffset = entityId & SparseSet<T>.PAGE_MASK;
                    ref int pageDataRefT = ref MemoryMarshal.GetArrayDataReference(pageT);
                    ref int pageDataRefE = ref MemoryMarshal.GetArrayDataReference(pageE);
                    idxT = Unsafe.Add(ref pageDataRefT, pageOffset);
                    idxE = Unsafe.Add(ref pageDataRefE, pageOffset);
                    if((idxT | idxE) < 0) continue;  

                    if(checkChangedK && Unsafe.Add(ref ticksK_Ref, index) <= lastSystemTick) continue;
                    if(checkChangedT && Unsafe.Add(ref ticksT_Ref, idxT) <= lastSystemTick) continue;
                    if(checkChangedE && Unsafe.Add(ref ticksE_Ref, idxE) <= lastSystemTick) continue;

                    idxK = index;                      
                    return true;
                }
            }
            return false;
        }

        public QueryItem Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new QueryItem(
                    ref Unsafe.Add(ref denseT_Ref, idxT), 
                    ref Unsafe.Add(ref denseE_Ref, idxE), 
                    ref Unsafe.Add(ref denseK_Ref, idxK), 
                    ref Unsafe.Add(ref ticksT_Ref, idxT), 
                    ref Unsafe.Add(ref ticksE_Ref, idxE), 
                    ref Unsafe.Add(ref ticksK_Ref, idxK), 
                    lastGlobalTick);
            }
        }
    }

}