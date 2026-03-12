using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
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
public ref struct Query<T> where T : struct
{
    readonly Span<T> dense;
    readonly Span<Entity> entities;
    public Query(SparseSet<T> sparseSet)
    {
        this.dense = CollectionsMarshal.AsSpan(sparseSet.GetDense());
        entities = CollectionsMarshal.AsSpan(sparseSet.GetEntities());
    }

    public void ForEach(QueryFunc<T> func)
    {
        for(int i = 0; i < dense.Length; i++)
        {
            func(ref dense[i]);
        }
    }
    
    public void ForEach(QueryFuncEntity<T> func)
    {
        for(int i = 0; i < dense.Length; i++)
        {
            func(entities[i],ref dense[i]);
        }
    }

    public void ForEach<IAction>(IAction action) where IAction : struct, IQueryAction<T>{
        for(int i = 0; i < dense.Length; i++)
        {
            action.Execute(ref dense[i]);
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
    ISparseSet sparseE;
    ISparseSet sparseT;

    
    public Query(SparseSet<T> s1, SparseSet<E> s2)
    {
        sparseE = s2;
        sparseT = s1;
    }   

    public void ForEach(QueryFunc<T,E> func)
    {
        SparseSet<T> s1 = (SparseSet<T>)sparseT;
        SparseSet<E> s2 = (SparseSet<E>)sparseE;
        var denseT = CollectionsMarshal.AsSpan(s1.GetDense());
        var denseE = CollectionsMarshal.AsSpan(s2.GetDense());
        if(sparseT.Size<sparseE.Size){
            var entities = CollectionsMarshal.AsSpan(s1.GetEntities());
            var entitiesE = s2.GetSparseSet().AsSpan();
            for(int i = 0; i < denseT.Length; i++)
            {
                int entityId = entities[i].Id;
                int indexE = entitiesE[entityId];
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
                if(indexE != -1){
                    func(ref denseT[indexE], ref denseE[i]);
                }   
            }
        }
    }

        public void ForEach(QueryFuncEntity<T,E> func)
    {
        SparseSet<T> s1 = (SparseSet<T>)sparseT;
        SparseSet<E> s2 = (SparseSet<E>)sparseE;
        var denseT = CollectionsMarshal.AsSpan(s1.GetDense());
        var denseE = CollectionsMarshal.AsSpan(s2.GetDense());
        if(sparseT.Size<sparseE.Size){
            var entities = CollectionsMarshal.AsSpan(s1.GetEntities());
            var entitiesE = s2.GetSparseSet().AsSpan();
            for(int i = 0; i < denseT.Length; i++)
            {
                int entityId = entities[i].Id;
                int indexE = entitiesE[entityId];
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
                if(indexE != -1){
                    func(entities[i], ref denseT[indexE], ref denseE[i]);
                }   
            }
        }
    }

    public void ForEach<IAction>(ref IAction action) 
    where IAction :struct, IQueryAction<T,E>
    {
        SparseSet<T> s1 = (SparseSet<T>)sparseT;
        SparseSet<E> s2 = (SparseSet<E>)sparseE;
        var denseT = CollectionsMarshal.AsSpan(s1.GetDense());
        var denseE = CollectionsMarshal.AsSpan(s2.GetDense());
        if(sparseT.Size<sparseE.Size){
            var entities = CollectionsMarshal.AsSpan(s1.GetEntities());
            var entitiesE = s2.GetSparseSet().AsSpan();
            for(int i = 0; i < denseT.Length; i++)
            {
                int entityId = entities[i].Id;
                int indexE = entitiesE[entityId];
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
                if(indexE != -1){
                    action.Execute(ref denseT[indexE], ref denseE[i]);
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
    ISparseSet sparseE;
    ISparseSet sparseT;
    ISparseSet sparseK;

    QueryFilter queryFilter;

    Span<Bitset> entitiesMask;
    
    public Query(SparseSet<T> s1, SparseSet<E> s2, SparseSet<K> s3, Span<Bitset> entitiesMask)
    {
        sparseE = s2;
        sparseT = s1;
        sparseK = s3;
        queryFilter = new QueryFilter();
        this.entitiesMask = entitiesMask;
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

    public void ForEach(QueryFunc<T,E,K> func)
    {
        SparseSet<T> s1 = (SparseSet<T>)sparseT;
        SparseSet<E> s2 = (SparseSet<E>)sparseE;
        SparseSet<K> s3 = (SparseSet<K>)sparseK;
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
        SparseSet<T> s1 = (SparseSet<T>)sparseT;
        SparseSet<E> s2 = (SparseSet<E>)sparseE;
        SparseSet<K> s3 = (SparseSet<K>)sparseK;
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
        SparseSet<T> s1 = (SparseSet<T>)sparseT;
        SparseSet<E> s2 = (SparseSet<E>)sparseE;
        SparseSet<K> s3 = (SparseSet<K>)sparseK;
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
}