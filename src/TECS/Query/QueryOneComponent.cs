

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TECS;
namespace TECS.Queries;

public ref struct QueryItem<T> where T: struct
{
    private Entity entity;
    private ref T Component;
    private ref ulong tick;
    private readonly ulong systemTick;

    public QueryItem(Entity entity, ref T component, ref ulong tick, ulong systemTick)
    {
        this.entity = entity;
        Component = ref component;
        this.tick = ref tick;
        this.systemTick = systemTick;
    }

    public ref readonly T Read => ref Component;
    public ref T Write
    {
        get
        {
            tick = systemTick;
            return ref Component;
        }
    }
    public Entity Entity => Entity;

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

    public ref T Single()
    {
        var enumerator = GetEnumerator();
        bool hasFirst = enumerator.MoveNext();
        #if DEBUG
        if (!hasFirst)
        {
            throw new InvalidCastException($"No {typeof(T).Name} has been added.");
        }
        #endif

        ref T firstResult = ref enumerator.Current.Write;


        #if DEBUG
        if (enumerator.MoveNext())
        {
            throw new InvalidOperationException($"More than one entity with queried component {typeof(T).Name} has been found!");
        }
        #endif
        return ref firstResult;
    }

    public readonly ref T SingleReadonly()
    {
        var enumerator = GetEnumerator();
        bool hasFirst = enumerator.MoveNext();
        #if DEBUG
        if (!hasFirst)
        {
            throw new InvalidCastException($"No {typeof(T).Name} has been added.");
        }
        #endif

        ref readonly T firstResult = ref enumerator.Current.Read;


        #if DEBUG
        if (enumerator.MoveNext())
        {
            throw new InvalidOperationException($"More than one entity with queried component {typeof(T).Name} has been found!");
        }
        #endif
        return ref firstResult;
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
            if(entityMask.Intersects(ref queryFilter.exludeMask)) continue;
            if(!entityMask.ContainsAll(ref queryFilter.includeMask)) continue;
            func(ref dense[i]);
        }
    }
    
    public void ForEach(QueryFuncEntity<T> func)
    {
        for(int i = 0; i < dense.Length; i++)
        {
            ref Bitset entityMask = ref entityMasks[entities[i].Id];
            if(entityMask.Intersects(ref queryFilter.exludeMask)) continue;
            if(!entityMask.ContainsAll(ref queryFilter.includeMask)) continue;
            func(entities[i],ref dense[i]);
        }
    }

    public void ForEach<IAction>(IAction action) where IAction : struct, IQueryAction<T>{
        for(int i = 0; i < dense.Length; i++)
        {
            ref Bitset entityMask = ref entityMasks[entities[i].Id];
            if(entityMask.Intersects(ref queryFilter.exludeMask)) continue;
            if(!entityMask.ContainsAll(ref queryFilter.includeMask)) continue;
            action.Execute(ref dense[i]);
        }
    }

    public unsafe readonly QueryEnumerator GetEnumerator()
    {
        // strip queryFilter of the readonly protection
        ref QueryFilter mutableFilter = ref Unsafe.AsRef(in queryFilter);
        //Now it can be passed into QueryEnumerator without problem
        QueryFilter* filter = (QueryFilter*)Unsafe.AsPointer(ref mutableFilter);
        return new QueryEnumerator(sparseSet, entityMasks, filter, lastSystemTick, lastGlobalTick, changed);
    }

    [StructLayout(LayoutKind.Auto)]
    public unsafe ref struct QueryEnumerator
    {
        private ref T dense;
        private readonly int denseLength;


        private ref Entity entities;
        private ref Bitset entityMasks;
        //TODO: Speed up how bitsets are used
        private ref Bitset includeFilter;
        private ref Bitset excludeFilter;
        private ref ulong ticks;
        private readonly ulong lastGlobalTick;
        private readonly ulong lastSystemTick;
        private int index;
        private readonly bool changed;

        private readonly bool hasInlcude;
        private readonly bool hasExclude;

        public QueryEnumerator(SparseSet<T> sparseSet, Span<Bitset> entityMasks, QueryFilter* queryFilter, ulong lastSystemTick, ulong lastGlobalTick, bool changed)
        {
            this.dense = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(sparseSet.GetDense()));
            this.entities = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(sparseSet.GetEntities()));
            this.entityMasks = ref MemoryMarshal.GetReference(entityMasks);
            this.includeFilter = ref queryFilter->includeMask;
            this.excludeFilter = ref queryFilter->exludeMask;
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
                    if(entityMask.Intersects(ref excludeFilter)) continue;
                    if(!entityMask.ContainsAll(ref includeFilter)) continue;
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
                if(hasExclude && entityMask.Intersects(ref excludeFilter)) continue;
                if(hasInlcude && !entityMask.ContainsAll(ref includeFilter)) continue;
                
                return true;
            }

            return false;
        }

        public QueryItem<T> Current{
            get{
                return new QueryItem<T>(
                    Unsafe.Add(ref entities, index),
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