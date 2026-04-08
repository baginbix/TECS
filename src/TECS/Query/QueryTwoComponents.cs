using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using src.Query;

namespace TECS.Query;


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
        private readonly Entity entity;

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
        public void Deconstruct(out Entity entity, out ComponentItem<T> comp1, out ComponentItem<E> comp2)
        {
            entity = this.entity;
            comp1 = new ComponentItem<T>(ref component1, ref tick1, globalTick);
            comp2 = new ComponentItem<E>(ref component2, ref tick2, globalTick);
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
        private ref E denseE;
        private ref T denseT;
        private ref Entity denseEntitites;
        private readonly int denseEntityLength;
        private ref int sparseEntitiesE;
        private ref int sparseEntitiesT;

        private readonly int maxEntT, maxEntE;

        private ref Bitset entitiesMask;
        private readonly QueryFilter queryFilter;

        private readonly bool tIsSmaller;
        private int index;
        private int idxT, idxE;

        private readonly ulong lastGlobalTick;   
        private readonly ulong lastSystemTick;     
        private ref ulong ticksT;
        private ref ulong ticksE;
        bool changedT = false;
        bool changedE = false;

        public QueryEnumerator(SparseSet<E> sparseE, SparseSet<T> sparseT, Span<Bitset> entitiesMask, QueryFilter queryFilter, ulong systemTick, ulong globalTick, bool changedT, bool changedE)
        {
            this.denseE = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(sparseE.GetDense()));
            this.denseT = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(sparseT.GetDense()));

            sparseEntitiesT = ref MemoryMarshal.GetReference(sparseT.GetSparseSet().AsSpan());
            sparseEntitiesE = ref MemoryMarshal.GetReference(sparseE.GetSparseSet().AsSpan());

            maxEntT = sparseT.GetSparseSet().Length;
            maxEntE = sparseE.GetSparseSet().Length;

            this.entitiesMask = ref MemoryMarshal.GetReference(entitiesMask);
            this.queryFilter = queryFilter;

            // Determine which set drives the loop
            tIsSmaller = sparseT.Size < sparseE.Size;

            if (tIsSmaller)
            {
                denseEntitites = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(sparseT.GetEntities()));
                denseEntityLength = sparseT.Size; 
            }
            else
            {
                denseEntitites = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(sparseE.GetEntities()));
                denseEntityLength = sparseE.Size;
            }
            index = -1;

            this.lastGlobalTick = globalTick;
            this.lastSystemTick = systemTick;

            ticksT = ref MemoryMarshal.GetReference(sparseT.GetLastTicks());
            ticksE = ref MemoryMarshal.GetReference(sparseE.GetLastTicks());

            this.changedT = changedT;
            this.changedE = changedE;
        }

        public bool MoveNext()
        {
            if(tIsSmaller)
            {

                while(++index < denseEntityLength)
                {
                    int entityId = Unsafe.As<Entity,int>(ref Unsafe.Add(ref denseEntitites, index));

                    if(entityId >= maxEntE) continue; // Skip invalid entities
                    Bitset entityMask = Unsafe.Add(ref entitiesMask, entityId);
                    if((queryFilter.exludeMask & entityMask) != 0) continue;
                    if((queryFilter.includeMask & entityMask) != queryFilter.includeMask) continue;
                    idxE = Unsafe.Add(ref sparseEntitiesE, entityId);
                    if(idxE == -1) continue; // Entity doesn't have component E
                    if(changedT && Unsafe.Add(ref ticksT, index) <= lastSystemTick) continue;
                    if(changedE && Unsafe.Add(ref ticksE, idxE) <= lastSystemTick) continue;

                    idxT = index;
                    return true;
                }
            }
            else
            {
                while(++index < denseEntityLength)
                {
                    int entityId = Unsafe.As<Entity,int>(ref Unsafe.Add(ref denseEntitites, index));

                    if(entityId >= maxEntT) continue; // Skip invalid entities
                    Bitset entityMask = Unsafe.Add(ref entitiesMask, entityId);
                    if((queryFilter.exludeMask & entityMask) != 0) continue;
                    if((queryFilter.includeMask & entityMask) != queryFilter.includeMask) continue;
                    idxT = Unsafe.Add(ref sparseEntitiesT, entityId);
                    if(idxT == -1) continue; // Entity doesn't have component E
                    if(changedT && Unsafe.Add(ref ticksT, idxT) <= lastSystemTick) continue;
                    if(changedE && Unsafe.Add(ref ticksE, index) <= lastSystemTick) continue;

                    idxE = index;
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
                    ref Unsafe.Add(ref denseT, idxT), 
                    ref Unsafe.Add(ref denseE, idxE), 
                    ref Unsafe.Add(ref ticksT, idxT), 
                    ref Unsafe.Add(ref ticksE, idxE), 
                    lastGlobalTick);
            }
        }
    }
}