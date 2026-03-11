using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace TECS
{
    public interface ISparseSet
    {
        int Size{ get; }
        void Remove(Entity entity);
        bool Contains(Entity entity);
        List<Entity> GetEntities();

        int[] GetSparseSet();
    }
    public class SparseSet<T> : ISparseSet
    {
        List<T> dense;
        List<Entity> denseEntities = new List<Entity>();
        int[] sparse;

        public int Size => dense.Count;
        public SparseSet():this(100){}
        public SparseSet(int size){
            dense = new List<T>(size);
            sparse =  new int[size];
            Array.Fill(sparse, -1);
        }
        public void Add(Entity entity, T data){
            if(sparse[entity.Id] != -1){
                dense[sparse[entity.Id]] = data;
                return;
            }         
            dense.Add(data);
            denseEntities.Add(entity);
            sparse[entity.Id] = dense.Count-1;
        }

        public void Remove(Entity entity){
            if(sparse[entity.Id] == -1){
                return;
            }   

            int denseId = sparse[entity.Id];
            dense[denseId] = dense[dense.Count-1];
            denseEntities[denseId] = denseEntities[dense.Count-1];
            
            dense.RemoveAt(dense.Count-1);
            denseEntities.RemoveAt(denseEntities.Count - 1);

            sparse[dense.Count] = denseId;
            sparse[entity.Id] = -1;
        }

        public T GetValue(Entity entity){
            return dense[sparse[entity.Id]];
        }

        public List<T> GetDense(){
            return dense;
        }

        public bool Contains(Entity entity)
        {
            return entity.Id < sparse.Length && sparse[entity.Id] != -1;
        }

        public List<Entity> GetEntities()
        {
            return denseEntities;
        }

        public int[] GetSparseSet() => sparse;
    }
}