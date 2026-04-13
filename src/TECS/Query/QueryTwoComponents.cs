
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TECS.Queries.Components;

namespace TECS.Queries;


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

        public QueryItem(Entity entity, ref T comp1, ref E comp2, ref ulong tick1, ref ulong tick2, ulong globalTick)
        {
            this.entity = entity;
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

        public ref readonly TRead Read<TRead>() where TRead : struct
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

        public ref TWrite Write<TWrite>() where TWrite : struct
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

    private QueryFilter queryFilter;
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
            var entities = s1.GetEntities();
            var entitiesE = s2.GetSparseSet().AsSpan();
            for(int i = 0; i < denseT.Length; i++)
            {
                int entityId = entities[i].Id;
                int pageIndex = entityId >> SparseSet<T>.PAGE_SHIFT;
                int pageOffset = entityId & SparseSet<T>.PAGE_MASK;
                int indexE = entitiesE[pageIndex][pageOffset];
                Bitset entityMask = entityMasks[entityId];
                if(entityMask.Intersects(ref queryFilter.exludeMask)) continue;
                if(!entityMask.ContainsAll(ref queryFilter.includeMask)) continue;
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
                int pageIndex = entityId >> SparseSet<E>.PAGE_SHIFT;
                int pageOffset = entityId & SparseSet<E>.PAGE_MASK;
                int indexE = entitiesE[pageIndex][pageOffset];
                Bitset entityMask = entityMasks[entityId];
                if(entityMask.Intersects(ref queryFilter.exludeMask)) continue;
                if(!entityMask.ContainsAll(ref queryFilter.includeMask)) continue;
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
                int pageIndex = entityId >> SparseSet<T>.PAGE_SHIFT;
                int pageOffset = entityId & SparseSet<T>.PAGE_MASK;
                int indexE = entitiesE[pageIndex][pageOffset];
                Bitset entityMask = entityMasks[entityId];
                if(entityMask.Intersects(ref queryFilter.exludeMask)) continue;
                if(!entityMask.ContainsAll(ref queryFilter.includeMask)) continue;
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
                int pageIndex = entityId >> SparseSet<E>.PAGE_SHIFT;
                int pageOffset = entityId & SparseSet<E>.PAGE_MASK;
                int indexE = entitiesE[pageIndex][pageOffset];
                Bitset entityMask = entityMasks[entityId];
                if(entityMask.Intersects(ref queryFilter.exludeMask)) continue;
                if(!entityMask.ContainsAll(ref queryFilter.includeMask)) continue;
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
                int pageIndex = entityId >> SparseSet<T>.PAGE_SHIFT;
                int pageOffset = entityId & SparseSet<T>.PAGE_MASK;
                int indexE = entitiesE[pageIndex][pageOffset];
                Bitset entityMask = entityMasks[entityId];
                if(entityMask.Intersects(ref queryFilter.exludeMask)) continue;
                if(!entityMask.ContainsAll(ref queryFilter.includeMask)) continue;

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
                int pageIndex = entityId >> SparseSet<E>.PAGE_SHIFT;
                int pageOffset = entityId & SparseSet<E>.PAGE_MASK;
                int indexE = entitiesE[pageIndex][pageOffset];
                Bitset entityMask = entityMasks[entityId];
                if(entityMask.Intersects(ref queryFilter.exludeMask)) continue;
                if(!entityMask.ContainsAll(ref queryFilter.includeMask)) continue;
                if(indexE != -1){
                    action.Execute(ref denseT[indexE], ref denseE[i]);
                }   
            }
        }
    }

public unsafe QueryEnumerator GetEnumerator() 
{
    QueryFilter* filterPtr = (QueryFilter*)Unsafe.AsPointer(ref queryFilter);
    return new QueryEnumerator(sparseE, sparseT, entityMasks, filterPtr, lastSystemTick, lastGlobalTick, changedT, changedE);
}

    [StructLayout(LayoutKind.Auto)]
    public unsafe ref struct QueryEnumerator
    {
        private ref E denseE;
        private ref T denseT;
        private ref Entity denseEntitites;
        private readonly int denseEntityLength;
        private ref int[] sparseEntitiesE;
        private ref int[] sparseEntitiesT;

        private ref int[] cachePageE;
        private ref int[] cachePageT;
        

        private ref Bitset entitiesMask;

        //TODO: Speed up how bitsets are used.
        private ref Bitset includeMask;
        private ref Bitset excludeMask;

        private readonly ulong lastGlobalTick;   
        private readonly ulong lastSystemTick;     
        private ref ulong ticksT;
        private ref ulong ticksE;
        private int index;
        private int idxT, idxE;
        private readonly bool tIsSmaller;
        bool changedT = false;
        bool changedE = false;
        bool hasInclude = false;
        bool hasExclude = false;

        private bool isCacheValid = false;

        public QueryEnumerator(SparseSet<E> sparseE, SparseSet<T> sparseT, Span<Bitset> entitiesMask, QueryFilter* queryFilter, ulong systemTick, ulong globalTick, bool changedT, bool changedE)
        {
            this.denseE = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(sparseE.GetDense()));
            this.denseT = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(sparseT.GetDense()));

            sparseEntitiesT = ref MemoryMarshal.GetReference(sparseT.GetSparseSet());
            sparseEntitiesE = ref MemoryMarshal.GetReference(sparseE.GetSparseSet());


            this.entitiesMask = ref MemoryMarshal.GetReference(entitiesMask);
            this.includeMask =  ref queryFilter->includeMask;
            this.excludeMask =  ref queryFilter->exludeMask;

            hasInclude = !includeMask.IsEmpty();
            hasExclude = !excludeMask.IsEmpty();

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            // OPTIMIZATION 3: Loop Unswitching
            if (changedT || changedE)
            {
                // SLOW PATH: Tick checking enabled
                if(tIsSmaller)
                {
                    while(++index < denseEntityLength)
                    {
                        // OPTIMIZATION 4: Safe ID fetch
                        int entityId = Unsafe.Add(ref denseEntitites, index).Id;

                        // 1. CHEAPEST CHECK FIRST: Resolve the pages
                        int pageIndex = entityId >> SparseSet<T>.PAGE_SHIFT;
                        if(pageIndex >= sparseEntitiesE.Length) continue;
                        int pageOffset = entityId & SparseSet<T>.PAGE_MASK;

                        int[] page = Unsafe.Add(ref sparseEntitiesT, pageIndex);
                        if(page == null) continue;

                        // 2. SECOND CHEAPEST: Check the sparse array
                        ref int pageDataRef = ref MemoryMarshal.GetArrayDataReference(page);
                        idxE = Unsafe.Add(ref pageDataRef, pageOffset);
                        if(idxE < 0) continue; 

                        // 3. HEAVY CHECK LAST: Only read the 128-byte mask if it actually exists in the sparse set!
                        // AND ONLY RUN THIS IF FILTERS ACTUALLY EXIST!
                        if (hasExclude || hasInclude)
                        {
                            ref Bitset entityMask = ref Unsafe.Add(ref entitiesMask, entityId);
                            if(hasExclude && entityMask.Intersects(ref excludeMask)) continue;
                            if(hasInclude && !entityMask.ContainsAll(ref includeMask)) continue;
                        }
                        
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
                        int entityId = Unsafe.Add(ref denseEntitites, index).Id;
                        //if(entityId >= maxEntT) continue;

                        // 1. CHEAPEST CHECK FIRST: Resolve the pages
                        int pageIndex = entityId >> SparseSet<T>.PAGE_SHIFT;
                        if(pageIndex >= sparseEntitiesT.Length) continue;
                        int pageOffset = entityId & SparseSet<T>.PAGE_MASK;

                        int[] page = Unsafe.Add(ref sparseEntitiesT, pageIndex);
                        if(page == null) continue;

                        // 2. SECOND CHEAPEST: Check the sparse array
                        ref int pageDataRef = ref MemoryMarshal.GetArrayDataReference(page);
                        idxT = Unsafe.Add(ref pageDataRef, pageOffset);
                        if(idxT < 0) continue; 

                        // 3. HEAVY CHECK LAST: Only read the 128-byte mask if it actually exists in the sparse set!
                        // AND ONLY RUN THIS IF FILTERS ACTUALLY EXIST!
                        if (hasExclude || hasInclude)
                        {
                            ref Bitset entityMask = ref Unsafe.Add(ref entitiesMask, entityId);
                            if(hasExclude && entityMask.Intersects(ref excludeMask)) continue;
                            if(hasInclude && !entityMask.ContainsAll(ref includeMask)) continue;
                        }
                        if(changedT && Unsafe.Add(ref ticksT, idxT) <= lastSystemTick) continue;
                        if(changedE && Unsafe.Add(ref ticksE, index) <= lastSystemTick) continue;

                        idxE = index;
                        return true;
                    }
                }
            }
            else
            {
                // FAST PATH: Zero tick checking overhead
                if(tIsSmaller)
                {
                    while(++index < denseEntityLength)
                    {
                        int entityId = Unsafe.Add(ref denseEntitites, index).Id;
                        //if(entityId >= maxEntT) continue;

                        // 1. CHEAPEST CHECK FIRST: Resolve the pages
                        int pageIndex = entityId >> SparseSet<T>.PAGE_SHIFT;
                        if(pageIndex >= sparseEntitiesE.Length) continue;
                        int pageOffset = entityId & SparseSet<T>.PAGE_MASK;

                        int[] page = Unsafe.Add(ref sparseEntitiesT, pageIndex);
                        if(page == null) continue;

                        // 2. SECOND CHEAPEST: Check the sparse array
                        ref int pageDataRef = ref MemoryMarshal.GetArrayDataReference(page);
                        idxE = Unsafe.Add(ref pageDataRef, pageOffset);
                        if(idxE < 0) continue; 

                        // 3. HEAVY CHECK LAST: Only read the 128-byte mask if it actually exists in the sparse set!
                        // AND ONLY RUN THIS IF FILTERS ACTUALLY EXIST!
                        if (hasExclude || hasInclude)
                        {
                            ref Bitset entityMask = ref Unsafe.Add(ref entitiesMask, entityId);
                            if(hasExclude && entityMask.Intersects(ref excludeMask)) continue;
                            if(hasInclude && !entityMask.ContainsAll(ref includeMask)) continue;
                        }

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
                        int entityId = Unsafe.Add(ref denseEntitites, index).Id;
                        //if(entityId >= maxEntT) continue;

                        // 1. CHEAPEST CHECK FIRST: Resolve the pages
                        int pageIndex = entityId >> SparseSet<T>.PAGE_SHIFT;
                        if(pageIndex >= sparseEntitiesT.Length) continue;
                        int pageOffset = entityId & SparseSet<T>.PAGE_MASK;

                        int[] page = Unsafe.Add(ref sparseEntitiesT, pageIndex);
                        if(page == null) continue;

                        // 2. SECOND CHEAPEST: Check the sparse array
                        ref int pageDataRef = ref MemoryMarshal.GetArrayDataReference(page);
                        idxT = Unsafe.Add(ref pageDataRef, pageOffset);
                        if(idxT < 0) continue;

                        // 3. HEAVY CHECK LAST: Only read the 128-byte mask if it actually exists in the sparse set!
                        // AND ONLY RUN THIS IF FILTERS ACTUALLY EXIST!
                        if (hasExclude || hasInclude)
                        {
                            ref Bitset entityMask = ref Unsafe.Add(ref entitiesMask, entityId);
                            if(hasExclude && entityMask.Intersects(ref excludeMask)) continue;
                            if(hasInclude && !entityMask.ContainsAll(ref includeMask)) continue;
                        }

                        if(changedT && Unsafe.Add(ref ticksT, idxT) <= lastSystemTick) continue;
                        if(changedE && Unsafe.Add(ref ticksE, index) <= lastSystemTick) continue;
    

                        idxE = index;
                        return true;
                    }
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
                    Unsafe.Add(ref denseEntitites, index),
                    ref Unsafe.Add(ref denseT, idxT), 
                    ref Unsafe.Add(ref denseE, idxE), 
                    ref Unsafe.Add(ref ticksT, idxT), 
                    ref Unsafe.Add(ref ticksE, idxE), 
                    lastGlobalTick);
            }
        }
    }
}