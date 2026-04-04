
using System.Reflection;
using System.Runtime.InteropServices;

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
        List<ulong> ticks;
        List<Entity> denseEntities = new List<Entity>();
        int[] sparse;
        private static readonly bool isTag = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length == 0;

        public int Size => dense.Count;
        public SparseSet(int size){
            dense = !isTag ? new List<T>(size) : new List<T>(0);
            sparse =  new int[size];
            Array.Fill(sparse, -1);
            ticks = new List<ulong>(size);
        }
        public void Add(Entity entity, T data, ulong currentTick){
            if(sparse[entity.Id] != -1){
                if(!isTag)
                    dense[sparse[entity.Id]] = data;
                ticks[entity.Id] = currentTick;
                return;
            }

            if (!isTag)
            {
                dense.Add(data);
                ticks.Add(currentTick);
            }
            denseEntities.Add(entity);
            sparse[entity.Id] = denseEntities.Count-1;
        }

        public void Remove(Entity entity){
            if(sparse[entity.Id] == -1){
                return;
            }   

            int denseId = sparse[entity.Id];
            int index = denseEntities.Count - 1;
            Entity lastEntity = denseEntities[index];

            if (!isTag)
            {
                dense[denseId] = dense[index];
                dense.RemoveAt(index);
            }
                
            denseEntities[denseId] = denseEntities[index];
            
            
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
        public OptionRef<T> GetValue(Entity entity, ulong currentTick){
            int index = sparse[entity.Id];
            if(index == -1)
                return  OptionRef<T>.None;
            ticks[entity.Id] = currentTick;
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

        public Span<ulong> GetLastTicks() => CollectionsMarshal.AsSpan(ticks);

        public int[] GetSparseSet() => sparse;
    }
}