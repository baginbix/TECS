using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace TECS
{
    public delegate void QueryFunc<T>(ref T comp);
    public delegate void QueryFunc<T,E>(ref T comp1, ref E comp2);

    public interface IQueryAction<T, E> where T: struct where E: struct
    {
        void Execute(ref T comp1, ref E comp2);
    }

    public interface IQueryAction<T> where T: struct
    {
        void Execute(ref T comp);
    }
    public ref struct Query<T> where T : struct
    {
        readonly Span<T> dense;
        public Query(SparseSet<T> sparseSet)
        {
            this.dense = CollectionsMarshal.AsSpan(sparseSet.GetDense());;
        }

        public void ForEach(QueryFunc<T> func)
        {
            for(int i = 0; i < dense.Length; i++)
            {
                func(ref dense[i]);
            }
        }

        public void ForEach<IAction>(IAction action) where IAction : struct, IQueryAction<T>{
            for(int i = 0; i < dense.Length; i++)
            {
                action.Execute(ref dense[i]);
            }
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
                    int entityId = entities[i];
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
                    int entityId = entities[i];
                    int indexE = entitiesE[entityId];
                    if(indexE != -1){
                        func(ref denseT[indexE], ref denseE[i]);
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
                    int entityId = entities[i];
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
                    int entityId = entities[i];
                    int indexE = entitiesE[entityId];
                    if(indexE != -1){
                        action.Execute(ref denseT[indexE], ref denseE[i]);
                    }   
                }
            }
        }
    }
}