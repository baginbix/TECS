
using System.Runtime.InteropServices;
using TECS;

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
            int index = dense.Count - 1;
            Entity lastEntity = denseEntities[index];

            dense[denseId] = dense[index];
            denseEntities[denseId] = denseEntities[index];
            
            dense.RemoveAt(index);
            denseEntities.RemoveAt(denseEntities.Count - 1);

            sparse[lastEntity.Id] = denseId;
            sparse[entity.Id] = -1;
        }
        /*
        public ref T GetValue(Entity entity){
            int index = sparse[entity.Id];
            return ref CollectionsMarshal.AsSpan(dense)[index];
        }
        */
        public OptionRef<T> GetValue(Entity entity){
            int index = sparse[entity.Id];
            if(index == -1)
                return  OptionRef<T>.None;
            return new OptionRef<T>(ref CollectionsMarshal.AsSpan(dense)[index]);
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