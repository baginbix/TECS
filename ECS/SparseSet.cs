using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECS.ECS
{
    public class SparseSet<T> 
    {
        List<T> dense;
        Dictionary<Entity,int> sparse = new Dictionary<Entity,int>();
        public SparseSet():this(100){}
        public SparseSet(int size){
            dense = new List<T>(size);
            sparse = new Dictionary<Entity,int>(size*4);
        }
        public void Add(int id, T data){
            if(sparse.ContainsKey(id)){
                dense[sparse[id]] = data;
                return;
            }         
            dense.Add(data);
            sparse[id] = dense.Count-1;
        }

        public void Remove(Entity id){
            int denseId = sparse[id];
            dense[denseId] = dense[dense.Count-1];
            dense.RemoveAt(dense.Count-1);
            int key = -1;
            foreach(var v in sparse){
                if(dense.Count == v.Value){
                    key = v.Key;
                }
            }
            sparse[key] = denseId;
        }

        public T GetValue(int id){
            return dense[sparse[id]];
        }

        public List<T> GetDense(){
            return dense;
        }
    }
}