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
    readonly QueryFilter queryFilter;
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
            Bitset entityMask = entityMasks[entities[i].Id];
            if((queryFilter.exludeMask & entityMask) != 0) continue;
            if((queryFilter.includeMask & entityMask) != queryFilter.includeMask) continue;
            func(ref dense[i]);
        }
    }
    
    public void ForEach(QueryFuncEntity<T> func)
    {
        for(int i = 0; i < dense.Length; i++)
        {
            Bitset entityMask = entityMasks[entities[i].Id];
            if((queryFilter.exludeMask & entityMask) != 0) continue;
            if((queryFilter.includeMask & entityMask) != queryFilter.includeMask) continue;
            func(entities[i],ref dense[i]);
        }
    }

    public void ForEach<IAction>(IAction action) where IAction : struct, IQueryAction<T>{
        for(int i = 0; i < dense.Length; i++)
        {
            Bitset entityMask = entityMasks[entities[i].Id];
            if((queryFilter.exludeMask & entityMask) != 0) continue;
            if((queryFilter.includeMask & entityMask) != queryFilter.includeMask) continue;
            action.Execute(ref dense[i]);
        }
    }

    public QueryEnumerator GetEnumerator() => new QueryEnumerator(sparseSet, entityMasks, queryFilter, lastSystemTick, lastGlobalTick, changed);
    

    public ref struct QueryEnumerator
    {
        private readonly Span<T> dense;
        private int index;

        private readonly Span<Entity> entities;
        private readonly Span<Bitset> entityMasks;
        private readonly QueryFilter queryFilter;
        private readonly SparseSet<T> sparseSet;
        private readonly Span<ulong> ticks;
        private readonly ulong lastGlobalTick;
        private readonly ulong lastSystemTick;
        private readonly bool changed;

        public QueryEnumerator(SparseSet<T> sparseSet, Span<Bitset> entityMasks, QueryFilter queryFilter, ulong lastSystemTick, ulong lastGlobalTick, bool changed)
        {
            this.dense = CollectionsMarshal.AsSpan(sparseSet.GetDense());
            this.entities = CollectionsMarshal.AsSpan(sparseSet.GetEntities());
            this.entityMasks = entityMasks;
            this.queryFilter = queryFilter;
            index = -1;

            this.sparseSet = sparseSet;
            this.lastSystemTick = lastSystemTick;
            this.lastGlobalTick = lastGlobalTick;

            this.changed = changed;

            this.ticks = sparseSet.GetLastTicks();
        }

        public bool MoveNext()
        {
            while(++index < dense.Length)
            {
                Bitset entityMask = entityMasks[entities[index].Id];
                if((queryFilter.exludeMask & entityMask) != 0) continue;
                if((queryFilter.includeMask & entityMask) != queryFilter.includeMask) continue;
                
                // If the component has been changed and the query is filtered to only include changed components, 
                // skip if the component hasn't been changed since the last time it was read
                if(changed && ticks[index] <= lastSystemTick) continue;
                return true;
            }

            return false;
        }

        public ComponentItem<T> Current{
            get{
                Span<ulong> ticks = sparseSet.GetLastTicks();
                return new ComponentItem<T>(ref dense[index], ref ticks[index], lastGlobalTick);
            }
        }
    }

    public Span<T> GetPacked()
    {
        return dense;
    }
}

public ref struct Query<T, E> 
where T: struct 
where  E: struct
{

    public readonly ref struct QueryItem
    {
        private readonly ref ulong tick1;
        private readonly ref ulong tick2;
        private readonly ulong globalTick;
        private readonly ref T component1;
        private readonly ref E component2;

        public QueryItem(ref T comp1, ref E comp2, ref ulong tick1, ref ulong tick2, ulong globalTick)
        {
            component1 = ref comp1;
            component2 = ref comp2;
            this.tick1 = ref tick1;
            this.tick2 = ref tick2;
            this.globalTick = globalTick;
        }

        public void Deconstruct(out ComponentItem<T> comp1, out ComponentItem<E> comp2)
        {
            comp1 = new ComponentItem<T>(ref component1, ref tick1, globalTick);
            comp2 = new ComponentItem<E>(ref component2, ref tick2, globalTick);
        }

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

            throw new InvalidOperationException($"Type {typeof(TWrite)} is not part of the query");
        }
    }
    SparseSet<E> sparseE;
    SparseSet<T> sparseT;

    private readonly Span<Bitset> entityMasks;

    private readonly QueryFilter queryFilter;
    private readonly ulong lastGlobalTick;  
    private readonly ulong lastSystemTick;

    private bool changedT = false;
    private bool changedE = false;


    
    public Query(SparseSet<T> s1, SparseSet<E> s2, Span<Bitset> entityMasks, ulong lastSystemTick, ulong lastGlobalTick)
    {
        sparseE = s2;
        sparseT = s1;
        this.entityMasks = entityMasks;
        this.lastGlobalTick = lastGlobalTick;
        this.lastSystemTick = lastSystemTick;
        queryFilter = new QueryFilter();
    }

    public Query<T,E> With<Component>()
    where Component: struct
    {
        queryFilter.With<Component>();
        return this;
    }

    public Query<T,E> Without<Component>()
    where Component: struct
    {
        queryFilter.Without<Component>();
        return this;
    }   
    public Query<T,E> Changed<TComponent>()
    where TComponent: struct
    {
        if(typeof(TComponent) == typeof(T))
        {
            changedT = true;
        }
        else if(typeof(TComponent) == typeof(E))
        {
            changedE = true;
        }
        #if DEBUG
        else
        {
            throw new InvalidOperationException($"Type {typeof(TComponent)} is not part of the query");
        }
        #endif

        return this;
    }

    public void ForEach(QueryFunc<T,E> func)
    {
        SparseSet<T> s1 = sparseT;
        SparseSet<E> s2 = sparseE;
        var denseT = CollectionsMarshal.AsSpan(s1.GetDense());
        var denseE = CollectionsMarshal.AsSpan(s2.GetDense());
        if(sparseT.Size<sparseE.Size){
            var entities = CollectionsMarshal.AsSpan(s1.GetEntities());
            var entitiesE = s2.GetSparseSet().AsSpan();
            for(int i = 0; i < denseT.Length; i++)
            {
                int entityId = entities[i].Id;
                int indexE = entitiesE[entityId];
                Bitset entityMask = entityMasks[entityId];
                if((queryFilter.exludeMask & entityMask) != 0) continue;
                if((queryFilter.includeMask & entityMask) != queryFilter.includeMask) continue;
                if(indexE != -1){
                    func(ref denseT[i], ref denseE[indexE]);
                }   
            }
        }
        else
        {
            var entities = CollectionsMarshal.AsSpan(s2.GetEntities());
            var entitiesE = s1.GetSparseSet().AsSpan();
            for(int i = 0; i < denseE.Length; i++)
            {
                int entityId = entities[i].Id;
                int indexE = entitiesE[entityId];
                Bitset entityMask = entityMasks[entityId];
                if((queryFilter.exludeMask & entityMask) != 0) continue;
                if((queryFilter.includeMask & entityMask) != queryFilter.includeMask) continue;
                if(indexE != -1){
                    func(ref denseT[indexE], ref denseE[i]);
                }   
            }
        }
    }

    public void ForEach(QueryFuncEntity<T,E> func)
    {
        SparseSet<T> s1 = sparseT;
        SparseSet<E> s2 = sparseE;
        var denseT = CollectionsMarshal.AsSpan(s1.GetDense());
        var denseE = CollectionsMarshal.AsSpan(s2.GetDense());
        if(sparseT.Size<sparseE.Size){
            var entities = CollectionsMarshal.AsSpan(s1.GetEntities());
            var entitiesE = s2.GetSparseSet().AsSpan();
            for(int i = 0; i < denseT.Length; i++)
            {
                int entityId = entities[i].Id;
                int indexE = entitiesE[entityId];
                Bitset entityMask = entityMasks[entityId];
                if((queryFilter.exludeMask & entityMask) != 0) continue;
                if((queryFilter.includeMask & entityMask) != queryFilter.includeMask) continue;
                if(indexE != -1){
                    func(entities[i],ref denseT[i], ref denseE[indexE]);
                }   
            }
        }
        else
        {
            var entities = CollectionsMarshal.AsSpan(s2.GetEntities());
            var entitiesE = s1.GetSparseSet().AsSpan();
            for(int i = 0; i < denseE.Length; i++)
            {
                int entityId = entities[i].Id;
                int indexE = entitiesE[entityId];
                Bitset entityMask = entityMasks[entityId];
                if((queryFilter.exludeMask & entityMask) != 0) continue;
                if((queryFilter.includeMask & entityMask) != queryFilter.includeMask) continue;
                if(indexE != -1){
                    func(entities[i], ref denseT[indexE], ref denseE[i]);
                }   
            }
        }
    }

    public void ForEach<IAction>(ref IAction action) 
    where IAction :struct, IQueryAction<T,E>
    {
        SparseSet<T> s1 = sparseT;
        SparseSet<E> s2 = sparseE;
        var denseT = CollectionsMarshal.AsSpan(s1.GetDense());
        var denseE = CollectionsMarshal.AsSpan(s2.GetDense());
        if(sparseT.Size<sparseE.Size){
            var entities = CollectionsMarshal.AsSpan(s1.GetEntities());
            var entitiesE = s2.GetSparseSet().AsSpan();
            for(int i = 0; i < denseT.Length; i++)
            {
                int entityId = entities[i].Id;
                int indexE = entitiesE[entityId];
                Bitset entityMask = entityMasks[entityId];
                if((queryFilter.exludeMask & entityMask) != 0) continue;
                if((queryFilter.includeMask & entityMask) != queryFilter.includeMask) continue;

                if(indexE != -1){
                    action.Execute(ref denseT[i], ref denseE[indexE]);
                }   
            }
        }
        else
        {
            var entities = CollectionsMarshal.AsSpan(s2.GetEntities());
            var entitiesE = s1.GetSparseSet().AsSpan();
            for(int i = 0; i < denseE.Length; i++)
            {
                int entityId = entities[i].Id;
                int indexE = entitiesE[entityId];
                Bitset entityMask = entityMasks[entityId];
                if((queryFilter.exludeMask & entityMask) != 0) continue;
                if((queryFilter.includeMask & entityMask) != queryFilter.includeMask) continue;
                if(indexE != -1){
                    action.Execute(ref denseT[indexE], ref denseE[i]);
                }   
            }
        }
    }

    public QueryEnumerator GetEnumerator() => new QueryEnumerator(sparseE, sparseT, entityMasks, queryFilter, lastSystemTick, lastGlobalTick, changedT, changedE);


    public ref struct QueryEnumerator
    {
        private readonly Span<E> denseE;
        private readonly Span<T> denseT;
        private readonly Span<Entity> denseEntitiesT; 
        private readonly Span<Entity> denseEntitiesE;
        private readonly Span<int> sparseEntitiesE;
        private readonly Span<int> sparseEntitiesT;

        private readonly Span<Bitset> entitiesMask;
        private readonly QueryFilter queryFilter;

        private readonly bool tIsSmaller;
        private int index;
        private int cachedIndex;

        private readonly ulong lastGlobalTick;   
        private readonly ulong lastSystemTick;     
        private readonly Span<ulong> ticksT;
        private readonly Span<ulong> ticksE;
        bool changedT = false;
        bool changedE = false;

        public QueryEnumerator(SparseSet<E> sparseE, SparseSet<T> sparseT, Span<Bitset> entitiesMask, QueryFilter queryFilter, ulong systemTick, ulong globalTick, bool changedT, bool changedE)
        {
            this.denseE = CollectionsMarshal.AsSpan(sparseE.GetDense());
            this.denseT = CollectionsMarshal.AsSpan(sparseT.GetDense());
            this.denseEntitiesT = CollectionsMarshal.AsSpan(sparseT.GetEntities());

            sparseEntitiesT = sparseT.GetSparseSet().AsSpan();
            sparseEntitiesE = sparseE.GetSparseSet().AsSpan();

            this.entitiesMask = entitiesMask;
            this.queryFilter = queryFilter;

            // Determine which set drives the loop
            tIsSmaller = sparseT.Size < sparseE.Size;

            if (tIsSmaller)
            {
                denseEntitiesT = CollectionsMarshal.AsSpan(sparseT.GetEntities());
                denseEntitiesE = default; // Unused
            }
            else
            {
                denseEntitiesE = CollectionsMarshal.AsSpan(sparseE.GetEntities());
                denseEntitiesT = default; // Unused
            }
            index = -1;

            this.lastGlobalTick = globalTick;
            this.lastSystemTick = systemTick;

            ticksT = sparseT.GetLastTicks();
            ticksE = sparseE.GetLastTicks();

            this.changedT = changedT;
            this.changedE = changedE;
        }

        public bool MoveNext()
        {
            if(tIsSmaller)
            {

                while(++index < denseT.Length)
                {
                    int entityId = denseEntitiesT[index].Id;
                    Bitset entityMask = entitiesMask[entityId];
                    if((queryFilter.exludeMask & entityMask) != 0) continue;
                    if((queryFilter.includeMask & entityMask) != queryFilter.includeMask) continue;
                    if(changedT && ticksT[index] <= lastGlobalTick) continue;
                    if(changedE && ticksE[sparseEntitiesE[entityId]] <= lastGlobalTick) continue;
                    if(entityId < sparseEntitiesE.Length && (cachedIndex = sparseEntitiesE[entityId]) != -1)
                    {

                        return true;
                    }
                }
            }
            else
            {
                while(++index < denseE.Length)
                {
                    int entityId = denseEntitiesE[index].Id;
                    Bitset entityMask = entitiesMask[entityId];
                    if((queryFilter.exludeMask & entityMask) != 0) continue;
                    if((queryFilter.includeMask & entityMask) != queryFilter.includeMask) continue;
                    if(changedE && ticksE[index] <= lastGlobalTick) continue;
                    if(changedT && ticksT[sparseEntitiesT[entityId]] <= lastGlobalTick) continue;
                    if(entityId < sparseEntitiesT.Length && (cachedIndex = sparseEntitiesT[entityId]) != -1)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public QueryItem Current
        {
            get
            {
                if(tIsSmaller)
                {
                    ref T comp1 = ref denseT[index];
                    ref E comp2 = ref denseE[cachedIndex];
                    return new QueryItem(ref comp1, ref comp2, ref ticksT[index], ref ticksE[cachedIndex], lastGlobalTick);
                }
                else
                {
                    ref T comp1 = ref denseT[cachedIndex];
                    ref E comp2 = ref denseE[index];
                    return new QueryItem(ref comp1, ref comp2, ref ticksT[cachedIndex], ref ticksE[index], lastGlobalTick);
                }
            }
        }
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
                Bitset entityMask = entitiesMask[entityId];
                if((queryFilter.exludeMask & entityMask) != 0) continue;
                if((queryFilter.includeMask & entityMask) != queryFilter.includeMask) continue;
                int indexE = entitiesE[entityId];
                int indexK = entitiesK[entityId];
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
                Bitset entityMask = entitiesMask[entityId];
                if((queryFilter.exludeMask & entityMask) != 0) continue;
                if((queryFilter.includeMask & entityMask) != queryFilter.includeMask) continue;
                int indexT = entitiesT[entityId];
                int indexK = entitiesK[entityId];
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
                Bitset entityMask = entitiesMask[entityId];
                if((queryFilter.exludeMask & entityMask) != 0) continue;
                if((queryFilter.includeMask & entityMask) != queryFilter.includeMask) continue;

                int indexT = entitiesT[entityId];
                int indexE = entitiesE[entityId];

                if(indexT != -1 && indexE != -1){
                    func(ref denseT[indexT], ref denseE[indexE], ref denseK[i]);
                }
            }
        }
    }

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
                Bitset entityMask = entitiesMask[entityId];
                if((queryFilter.exludeMask & entityMask) != 0) continue;
                if((queryFilter.includeMask & entityMask) != queryFilter.includeMask) continue;
                int indexE = entitiesE[entityId];
                int indexK = entitiesK[entityId];
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
                Bitset entityMask = entitiesMask[entityId];
                if((queryFilter.exludeMask & entityMask) != 0) continue;
                if((queryFilter.includeMask & entityMask) != queryFilter.includeMask) continue;
                int indexT = entitiesT[entityId];
                int indexK = entitiesK[entityId];
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
                Bitset entityMask = entitiesMask[entityId];
                if((queryFilter.exludeMask & entityMask) != 0) continue;
                if((queryFilter.includeMask & entityMask) != queryFilter.includeMask) continue;

                int indexT = entitiesT[entityId];
                int indexE = entitiesE[entityId];

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
                Bitset entityMask = entitiesMask[entityId];
                if((queryFilter.exludeMask & entityMask) != 0) continue;
                if((queryFilter.includeMask & entityMask) != queryFilter.includeMask) continue;

                int indexE = entitiesE[entityId];
                int indexK = entitiesK[entityId];
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
                Bitset entityMask = entitiesMask[entityId];
                if((queryFilter.exludeMask & entityMask) != 0) continue;
                if((queryFilter.includeMask & entityMask) != queryFilter.includeMask) continue;
                int indexT = entitiesT[entityId];
                int indexK = entitiesK[entityId];
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
                Bitset entityMask = entitiesMask[entityId];
                if((queryFilter.exludeMask & entityMask) != 0) continue;
                if((queryFilter.includeMask & entityMask) != queryFilter.includeMask) continue;
                int indexT = entitiesT[entityId];
                int indexE = entitiesE[entityId];
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

        public ref readonly T ReadComponent1 => ref component1;
        public ref readonly E ReadComponent2 => ref component2;
        public ref readonly K ReadComponent3 => ref component3; 

        public ref T WriteComponent1
        {
            get
            {
                tick1 = globalTick;
                return ref component1;
            }
        }

        public ref E WriteComponent2
        {
            get
            {
                tick2 = globalTick;
                return ref component2;
            }
        }

        public ref K WriteComponent3
        {
            get
            {
                tick3 = globalTick;
                return ref component3;
            }
        }

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


    public ref struct QueryEnumerator
    {
        enum SmallestSet { T, E, K }
        private readonly Span<E> denseE;
        private readonly Span<T> denseT;
        private readonly Span<K> denseK;
        private readonly Span<Entity> denseEntitiesT; 
        private readonly Span<Entity> denseEntitiesE;
        private readonly Span<Entity> denseEntitiesK;
        private readonly Span<int> sparseEntitiesE;
        private readonly Span<int> sparseEntitiesT;
        private readonly Span<int> sparseEntitiesK;

        private readonly SmallestSet smallestSet;
        private int index;
        private int cachedIndex1;
        private int cachedIndex2;

        private readonly QueryFilter queryFilter;
        private readonly Span<Bitset> entitiesMask;

        private readonly ulong lastSystemTick;
        private readonly ulong lastGlobalTick;
        private readonly Span<ulong> ticksT;
        private readonly Span<ulong> ticksE;
        private readonly Span<ulong> ticksK;

        ChangedSet changedMask;

        public QueryEnumerator(SparseSet<E> sparseE, SparseSet<T> sparseT, SparseSet<K> sparseK, Span<Bitset> entitiesMask, QueryFilter queryFilter, ulong lastSystemTick, ulong lastGlobalTick, ChangedSet changedMask)
        {
            this.denseE = CollectionsMarshal.AsSpan(sparseE.GetDense());
            this.denseT = CollectionsMarshal.AsSpan(sparseT.GetDense());
            this.denseK = CollectionsMarshal.AsSpan(sparseK.GetDense());

            sparseEntitiesT = sparseT.GetSparseSet().AsSpan();
            sparseEntitiesE = sparseE.GetSparseSet().AsSpan();
            sparseEntitiesK = sparseK.GetSparseSet().AsSpan();

            this.ticksT = sparseT.GetLastTicks();
            this.ticksE = sparseE.GetLastTicks();
            this.ticksK = sparseK.GetLastTicks();

            this.lastSystemTick = lastSystemTick;
            this.lastGlobalTick = lastGlobalTick;

            this.entitiesMask = entitiesMask;
            this.queryFilter = queryFilter;

            this.changedMask = changedMask;

            // Determine which set drives the loop
            if(sparseT.Size < sparseE.Size && sparseT.Size < sparseK.Size)
            {
                smallestSet = SmallestSet.T;
            }
            else if(sparseE.Size < sparseK.Size)
            {
                smallestSet = SmallestSet.E;
            }
            else
            {
                smallestSet = SmallestSet.K;
            }

            if (smallestSet == SmallestSet.T)
            {
                denseEntitiesT = CollectionsMarshal.AsSpan(sparseT.GetEntities());
                denseEntitiesE = default; // Unused
                denseEntitiesK = default; // Unused
            }
            else if (smallestSet == SmallestSet.E)
            {
                denseEntitiesE = CollectionsMarshal.AsSpan(sparseE.GetEntities());
                denseEntitiesT = default; // Unused
                denseEntitiesK = default; // Unused
            }
            else 
            {
                denseEntitiesK = CollectionsMarshal.AsSpan(sparseK.GetEntities());
                denseEntitiesT = default; // Unused
                denseEntitiesE = default; // Unused
            }
            index = -1;
        }

        public bool MoveNext()
        {
            bool checkChangedT = (changedMask & ChangedSet.T) != 0;
            bool checkChangedE = (changedMask & ChangedSet.E) != 0;
            bool checkChangedK = (changedMask & ChangedSet.K) != 0;
            if(smallestSet == SmallestSet.T)
            {

                while(++index < denseT.Length)
                {
                    int entityId = denseEntitiesT[index].Id;
                    Bitset entityMask = entitiesMask[entityId];
                    if((queryFilter.exludeMask & entityMask) != 0) continue;
                    if((queryFilter.includeMask & entityMask) != queryFilter.includeMask) continue;
                    if(checkChangedT && ticksT[index] <= lastSystemTick) continue;
                    if(checkChangedE && ticksE[sparseEntitiesE[entityId]] <= lastSystemTick) continue;
                    if(checkChangedK && ticksK[sparseEntitiesK[entityId]] <= lastSystemTick) continue;
                    if(entityId< sparseEntitiesE.Length && entityId < sparseEntitiesK.Length )
                    {
                        cachedIndex1 = sparseEntitiesE[entityId];
                        cachedIndex2 = sparseEntitiesK[entityId];
                        if(cachedIndex1 != -1 && cachedIndex2 != -1)
                        {                            
                            return true;
                        }
                    }
                }
            }
            else if(smallestSet ==  SmallestSet.E)
            {
                while(++index < denseE.Length)
                {
                    int entityId = denseEntitiesE[index].Id;
                    Bitset entityMask = entitiesMask[entityId];
                    if((queryFilter.exludeMask & entityMask) != 0) continue;
                    if((queryFilter.includeMask & entityMask) != queryFilter.includeMask) continue;
                    if(checkChangedE && ticksE[index] <= lastSystemTick) continue;
                    if(checkChangedT && ticksT[sparseEntitiesT[entityId]] <= lastSystemTick) continue;
                    if(checkChangedK && ticksK[sparseEntitiesK[entityId]] <= lastSystemTick) continue;
                    if(entityId< sparseEntitiesT.Length && entityId < sparseEntitiesK.Length )
                    {
                        cachedIndex1 = sparseEntitiesT[entityId];
                        cachedIndex2 = sparseEntitiesK[entityId];
                        if(cachedIndex1 != -1 && cachedIndex2 != -1)
                        {                            
                            return true;
                        }
                    }
                }
            }
            else
            {
                while(++index < denseK.Length)
                {
                    int entityId = denseEntitiesK[index].Id;
                    Bitset entityMask = entitiesMask[entityId];
                    if((queryFilter.exludeMask & entityMask) != 0) continue;
                    if((queryFilter.includeMask & entityMask) != queryFilter.includeMask) continue;
                    if(checkChangedK && ticksK[index] <= lastSystemTick) continue;
                    if(checkChangedT && ticksT[sparseEntitiesT[entityId]] <= lastSystemTick) continue;
                    if(checkChangedE && ticksE[sparseEntitiesE[entityId]] <= lastSystemTick) continue;
                    if(entityId< sparseEntitiesT.Length && entityId < sparseEntitiesE.Length )
                    {
                        cachedIndex1 = sparseEntitiesT[entityId];
                        cachedIndex2 = sparseEntitiesE[entityId];
                        if(cachedIndex1 != -1 && cachedIndex2 != -1)
                        {                            
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public QueryItem Current
        {
            get
            {
                if(smallestSet == SmallestSet.T)
                {
                    ref T comp1 = ref denseT[index];
                    ref E comp2 = ref denseE[cachedIndex1];
                    ref K comp3 = ref denseK[cachedIndex2];
                    return new QueryItem(ref comp1, ref comp2, ref comp3, ref ticksT[index], ref ticksE[cachedIndex1], ref ticksK[cachedIndex2], lastGlobalTick);
                }
                else if(smallestSet == SmallestSet.E)
                {
                    ref T comp1 = ref denseT[cachedIndex1];
                    ref E comp2 = ref denseE[index];
                    ref K comp3 = ref denseK[cachedIndex2];
                    return new QueryItem(ref comp1, ref comp2, ref comp3, ref ticksT[cachedIndex1], ref ticksE[index], ref ticksK[cachedIndex2], lastGlobalTick);
                }
                else
                {
                    ref T comp1 = ref denseT[cachedIndex1];
                    ref E comp2 = ref denseE[cachedIndex2];
                    ref K comp3 = ref denseK[index];
                    return new QueryItem(ref comp1, ref comp2, ref comp3, ref ticksT[cachedIndex1], ref ticksE[cachedIndex2], ref ticksK[index], lastGlobalTick);
                }
            }
        }
    }

}