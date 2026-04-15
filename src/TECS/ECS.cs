using TECS.Queries;
using TECS.Components;
using src.Event;
using TECS.Event;

namespace TECS
{

    public interface IEngine
    {
        Query<T> Query<T>() where T : struct;
        Query<T, E> Query<T, E>() where T : struct where E : struct;
        Query<T, E, K> Query<T, E, K>() where T : struct where E : struct where K : struct;
        EventWriter<TEvent> GetEventWriter<TEvent>() where TEvent : struct;
        EventReader<TEvent> ReadEvents<TEvent>() where TEvent : struct;
        OptionRef<T> QueryComponent<T>(Entity entity) where T: struct;
        Option<T> QueryReadonlyComponent<T>(Entity entity) where T : struct;
    }

    public class ECS : IEngine
    {
        public ulong GlobalTick { get; private set; } = 0;
        EntityManager entityManager;
        ISparseSet[] components;

        Dictionary<Bitset, SparseSet<Entity>> groups;


        private Bitset[] entityMasks;

        Dictionary<Type, IResource> resources;

        Dictionary<Type, IEventWriter> cachedWriters;
        Dictionary<(Type systemType,Type eventType), IEventReader> cachedReaders;
        EventManager eventManager = new();

        [ThreadStatic]private static Type activeSystem;
        [ThreadStatic]private static ulong currentSystemLastTick = 0;
        

        bool stop = false;

        public ECS()
        {
            int maxEntityCount = 1000;
            components = new ISparseSet[100];
            entityManager = new();
            groups = new();
            entityMasks = new Bitset[maxEntityCount];
            resources = new();
            cachedWriters = new Dictionary<Type, IEventWriter>();
            cachedReaders = new Dictionary<(Type systemType, Type eventType), IEventReader>();

        }

        public void NextTick()
        {
            GlobalTick++;
        }
        public void SetActiveSystem(Type system) => activeSystem = system;
        public void SetLastSystemTick(ulong tick) => currentSystemLastTick = tick;
        public Entity CreateEntity()
        {
            Entity entity = entityManager.GetId();
            if (entity.Id >= entityMasks.Length)
            {
                Array.Resize(ref entityMasks, entityMasks.Length * 2);
            }
            entityMasks[entity.Id] = new();
            return entity;
        }

        public bool IsEntityAlive(Entity entity)
        {
            return entityManager.IsAlive(entity);
        }


        public void InsertResource<T>(T newResource) where T:IResource
        {
            resources.Add(typeof(T), newResource);
        }
        public void InsertResource<T>() where T:IResource, new()
        {
            resources.Add(typeof(T), new T());
        }

        public T GetResource<T>()
        {
            return (T)resources[typeof(T)];
        }

        public void InsertComponent<T>(Entity entityId, T component) where T : struct
        {
            int typeId = ComponentID<T>.Value;
            SparseSet<T> set = GetOrCreateSet<T>();

            set.Add(entityId, component, GlobalTick);
            entityMasks[entityId.Id].SetBit(typeId);
        }

        public SparseSet<T> GetOrCreateSet<T>() where T : struct
        {
            int typeID = ComponentID<T>.Value;
            if (typeID >= components.Length)
            {
                Array.Resize(ref components, Math.Max(typeID + 1, components.Length * 2));
            }
            if (components[typeID] == null)
            {
                components[typeID] = new SparseSet<T>(1000);
                ComponentRegistry.Register(typeof(T));
            }

            return (SparseSet<T>)components[typeID];
        }

        public List<T> QueryComponent<T>() where T : struct
        {
            return GetOrCreateSet<T>().GetDense();
        }

        public EntityQueryData<T,E,K> QueryEntity<T,E,K>(Entity entity)
        where T:struct
        where E:struct
        where K:struct
        {
            var optT = GetOrCreateSet<T>().GetValue(entity, GlobalTick);
            var optE = GetOrCreateSet<E>().GetValue(entity, GlobalTick);
            var optK = GetOrCreateSet<K>().GetValue(entity, GlobalTick);

            if(optT.IsNone || optE.IsNone || optK.IsNone)
            {
                return EntityQueryData<T,E,K>.None;
            }
            EntityQueryData<T,E,K> data = new( 
                ref optT.Unwrap(),
                ref optE.Unwrap(),
                ref optK.Unwrap()
            );
            return data;
        }

        public void DestroyEntity(Entity entity)
        {
            Bitset bitset = entityMasks[entity.Id];

            for (int i = 0; i < components.Length; i++)
            {
                if (bitset.HasBit(i))
                {
                    components[i].Remove(entity);
                }
            }

            entityMasks[entity.Id].ClearBits();
            
            //Release ID back to EntityManager
            entityManager.Free(entity);
        }

        public void RemoveComponent<T>(Entity entityId) where T : struct
        {
            SparseSet<T> set = GetOrCreateSet<T>();
            set.Remove(entityId);
            entityMasks[entityId.Id].ClearBit(ComponentID<T>.Value);
        }

        private void AddToGroup(Entity entity)
        {
            Bitset bitset = entityMasks[entity.Id];
            if (!groups.TryGetValue(bitset, out var group))
            {
                group = new SparseSet<Entity>(1000);
                groups.Add(bitset, group);
            }
            group.Add(entity, entity, GlobalTick);
        }

        public OptionRef<T> QueryComponent<T>(Entity entity) where T:struct
        {
            var set = GetOrCreateSet<T>();
            return  set.GetValue(entity, GlobalTick);
        }

        public Option<T> QueryReadonlyComponent<T>(Entity entity) where T : struct
        {
            return GetOrCreateSet<T>().GetReadonlyValue(entity);
        }

        public Query<T> Query<T>()
        where T : struct
        {
            return new Query<T>(GetOrCreateSet<T>(),entityMasks, currentSystemLastTick, GlobalTick);
        }

        public Query<T, E> Query<T, E>()
        where T : struct
        where E : struct
        {
            return new Query<T, E>(GetOrCreateSet<T>(), GetOrCreateSet<E>(), entityMasks,currentSystemLastTick, GlobalTick);
        }

        public Query<T, E, K> Query<T, E, K>()
        where T : struct
        where E : struct
        where K : struct
        {
            return new Query<T, E, K>(GetOrCreateSet<T>(), GetOrCreateSet<E>(), GetOrCreateSet<K>(), entityMasks, currentSystemLastTick, GlobalTick);
        }

        public List<T> GetComponentList<T>() where T : struct
        {
            return GetOrCreateSet<T>().GetDense();
        }

        [Obsolete("This method had been depracated. Use ECS.GetEventWriter()")]
        internal void SendEvent<TEvent>(in TEvent @event) where TEvent: struct
        {
            eventManager.SendEvent<TEvent>(in @event);
        }

        public EventWriter<TEvent> GetEventWriter<TEvent>() where TEvent:struct
        {
            if(!cachedWriters.TryGetValue(typeof(TEvent), out var writer))
            {
                writer = new EventWriter<TEvent>(eventManager);
                cachedWriters.Add(typeof(TEvent), writer);
            }
            return (EventWriter<TEvent>)writer;
        }

        public EventReader<TEvent> ReadEvents<TEvent>() where TEvent: struct
        {
            if (!cachedReaders.TryGetValue((activeSystem, typeof(TEvent)), out var reader))
            {
                reader = new EventReader<TEvent>(eventManager);
                cachedReaders.Add((activeSystem, typeof(TEvent)), reader);
            }
             
            return (EventReader<TEvent>)reader;
        }

        public void Flush()
        {
            eventManager.Flush();
        }

    }
} 