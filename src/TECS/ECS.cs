using TECS.Queries;
using TECS.Components;
using TECS.Event;
using TECS.Event;

namespace TECS
{

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class ECSSystemAttribute: Attribute{}

    public interface IEngine
    {
        EventWriter<TEvent> GetEventWriter<TEvent>() where TEvent : struct;
        EventReader<TEvent> GetEventReader<TEvent>() where TEvent : struct;
        OptionRef<T> QueryComponent<T>(Entity entity) where T: struct;
        Option<T> QueryReadonlyComponent<T>(Entity entity) where T : struct;
        T GetResource<T>() where T:IResource;
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

        public T GetResource<T>() where T :IResource
        {
            #if DEBUG
            if(resources.TryGetValue(typeof(T),  out var value))
            {
                return (T)value;
            }
            throw new InvalidOperationException($"The resource of type {typeof(T).Name} has not been added to the ECS!");
            # else
            return (T)resources[typeof(T)];
            #endif
        }

        public void InsertComponent<T>(Entity entityId, T component) where T : struct
        {
            int typeId = ComponentID<T>.Value;
            SparseSet<T> set = GetOrCreateSet<T>();

            set.Add(entityId, component, (uint)GlobalTick);
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
            var optT = GetOrCreateSet<T>().GetValue(entity, (uint)GlobalTick);
            var optE = GetOrCreateSet<E>().GetValue(entity, (uint)GlobalTick);
            var optK = GetOrCreateSet<K>().GetValue(entity, (uint)GlobalTick);

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


        public OptionRef<T> QueryComponent<T>(Entity entity) where T:struct
        {
            var set = GetOrCreateSet<T>();
            return  set.GetValue(entity, (uint)GlobalTick);
        }

        public Option<T> QueryReadonlyComponent<T>(Entity entity) where T : struct
        {
            return GetOrCreateSet<T>().GetReadonlyValue(entity);
        }

        public SparseSet<T> GetSparseSet<T>() where T: struct => GetOrCreateSet<T>();

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

        public EventReader<TEvent> GetEventReader<TEvent>() where TEvent: struct
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