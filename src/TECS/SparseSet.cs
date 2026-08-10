
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TECS
{
    public interface ISparseSet
    {
        int Size{ get; }
        void Remove(Entity entity);
        bool Contains(Entity entity);
        List<Entity> GetEntities();

        int[][] GetSparseSet();
    }
    public class SparseSet<T> : ISparseSet
    {
        // A page must be a power of 2 for the bitwise operations to work correctly.
        public const int PAGE_SHIFT = 12;
        private const int PAGE_SIZE = 1 << PAGE_SHIFT;
        public const int PAGE_MASK = PAGE_SIZE - 1;

        List<T> dense;
        List<uint> ticks;
        List<Entity> denseEntities = new List<Entity>();
        int[][] sparse;
        private static readonly bool isTag = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length == 0;

        public int Size => dense.Count;
        public SparseSet(int size){
            dense = !isTag ? new List<T>(size) : new List<T>(0);
            
            int numPages = (size + PAGE_SIZE - 1) >> PAGE_SHIFT;
            sparse =  new int[numPages][];
            ticks = new List<uint>(size);
        }
        public void Add(Entity entity, T data, uint currentTick){
            int pageIndex = entity.Id >> PAGE_SHIFT;
            int pageOffset = entity.Id & PAGE_MASK;
            
            if(pageIndex >= sparse.Length){
                int newNumPages = pageIndex + 1;
                Array.Resize(ref sparse, newNumPages);
            }

            if (sparse[pageIndex] is null)
            {
                sparse[pageIndex] = new int[PAGE_SIZE];
                Array.Fill(sparse[pageIndex], -1);
            }
            if(sparse[pageIndex][pageOffset] != -1){
                if(!isTag)
                    dense[sparse[pageIndex][pageOffset]] = data;
                ticks[entity.Id] = currentTick;
                return;
            }

            if (!isTag)
            {
                dense.Add(data);
                ticks.Add(currentTick);
            }
            denseEntities.Add(entity);
            sparse[pageIndex][pageOffset] = denseEntities.Count-1;
        }

        public void Remove(Entity entity){
            int pageIndex = entity.Id >> PAGE_SHIFT;
            int pageOffset = entity.Id & PAGE_MASK;

            if(sparse[pageIndex][pageOffset] == -1){
                return;
            }   

            int denseId = sparse[pageIndex][pageOffset];
            int index = denseEntities.Count - 1;
            Entity lastEntity = denseEntities[index];

            if (!isTag)
            {
                dense[denseId] = dense[index];
                dense.RemoveAt(index);
            }
                
            denseEntities[denseId] = denseEntities[index];
            
            
            denseEntities.RemoveAt(denseEntities.Count - 1);

            int pageIndexLast = lastEntity.Id >> PAGE_SHIFT;
            int pageOffsetLast = lastEntity.Id & PAGE_MASK;
            if(pageIndex >= sparse.Length || sparse[pageIndexLast] == null) return;
            
            sparse[pageIndexLast][pageOffsetLast] = denseId;
            sparse[pageIndex][pageOffset] = -1;
        }
        /*
        public ref T GetValue(Entity entity){
            int index = sparse[entity.Id];
            return ref CollectionsMarshal.AsSpan(dense)[index];
        }
        */
        public OptionRef<T> GetValue(Entity entity, uint currentTick){
            int pageIndex = entity.Id >> PAGE_SHIFT;
            int pageOffset = entity.Id & PAGE_MASK;
            if(pageIndex >= sparse.Length || sparse[pageIndex] == null)
                return OptionRef<T>.None;

            int index = sparse[pageIndex][pageOffset];
            if(index == -1)
                return  OptionRef<T>.None;
            ticks[entity.Id] = currentTick;
            return new OptionRef<T>(ref CollectionsMarshal.AsSpan(dense)[index]);
        }
        
        public Option<T> GetReadonlyValue(Entity entity){
            int pageIndex = entity.Id >> PAGE_SHIFT;
            int pageOffset = entity.Id & PAGE_MASK;
            if(pageIndex >= sparse.Length || sparse[pageIndex] == null)
                return Option<T>.None;

            int index = sparse[pageIndex][pageOffset];
            if(index == -1)
                return  Option<T>.None;
            return new Option<T>(ref CollectionsMarshal.AsSpan(dense)[index]);
        }

        public List<T> GetDense(){
            return dense;
        }

        public bool Contains(Entity entity)
        {
            int pageIndex = entity.Id >> PAGE_SHIFT;
            int pageOffset = entity.Id & PAGE_MASK;
            return entity.Id < sparse.Length && 
                   sparse[pageIndex] != null && 
                   sparse[pageIndex][pageOffset] != -1;
        }

        public List<Entity> GetEntities()
        {
            return denseEntities;
        }

        public Span<uint> GetLastTicks() => CollectionsMarshal.AsSpan(ticks);

        public int[][] GetSparseSet() => sparse;
    }
}