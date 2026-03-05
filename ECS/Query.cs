using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ECS.ECS
{
    public delegate void QueryFunc<T,E>(ref T comp1, ref E comp2);

    public interface IQueryAction<T, E> where T: struct where E: struct
    {
        void Execute(ref T comp1, ref E comp2);
    }
    ref struct Query<T> where T : struct
    {
        public T[] query;
        public Query()
        {

        }
    }

    ref struct Query<T, E> 
    where T: struct 
    where  E: struct
    {
        public Span<T> denseT;
        public Span<E> denseE;
        public Span<Entity> entities;

        public int[] sparseE;

        
        public Query(SparseSet<T> s1, SparseSet<E> s2)
        {
            denseT = CollectionsMarshal.AsSpan(s1.GetDense());
            denseE = CollectionsMarshal.AsSpan(s2.GetDense());
            entities = CollectionsMarshal.AsSpan(s1.GetEntities());

            sparseE = s2.GetSparseSet();
        }   

        public void ForEach(QueryFunc<T,E> func)
        {
            for(int i = 0; i < denseT.Length; i++)
            {
                int entityId = entities[i];
                int indexE = sparseE[entityId];
                if(indexE != -1){
                    func(ref denseT[i], ref denseE[indexE]);
                }   
            }
        }

        public void ForEach<IAction>(ref IAction action) 
        where IAction :struct, IQueryAction<T,E>
        {
            for(int i = 0; i < denseT.Length; i++)
            {
                int entityId = entities[i];
                int indexE = sparseE[entityId];
                if(indexE != -1){
                    action.Execute(ref denseT[i], ref denseE[indexE]);
                }   
            }
        }
    }
}