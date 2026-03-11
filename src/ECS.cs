using System;
using System.Collections.Generic;
using Elev.Documents.GitHub.ECS.src.Query;
using TECS.Commands;
using TECS.Components;

namespace TECS
{
    public class ECS
    {
        readonly int MaxEntityCount = 1_000_000;
        EntityManager entityManager;
        ISparseSet[] components;

        Dictionary<Bitset, SparseSet<Entity>> groups;

        ComponentBitRegistry componentBitRegistry;

        Bitset[] entityMasks;

        List<ISystem> systems;

        Dictionary<Type, object> resources;
        CommandBuffer commandBuffer = new();

        public ECS(int maxEntityCount = 1000)
        {
            components = new ISparseSet[100];
            entityManager = new();
            systems = new();
            groups = new();
            componentBitRegistry = new();
            entityMasks = new Bitset[1000];
            resources = new();
            MaxEntityCount = maxEntityCount;

        }

        public ECS() : this(1_000) { }

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

        public void AddSystem(ISystem system)
        {
            systems.Add(system);
        }

        public void InsertResource<T>(T newResource)
        {
            resources.Add(typeof(T), newResource);
        }

        public T GetResource<T>()
        {
            return (T)resources[typeof(T)];
        }

        public void InsertComponent<T>(Entity entityId, T component) where T : struct
        {
            int typeId = ComponentID<T>.Value;
            SparseSet<T> set = GetOrCreateSet<T>();

            set.Add(entityId, component);
            entityMasks[entityId.Id].SetBit(typeId);
        }

        private SparseSet<T> GetOrCreateSet<T>() where T : struct
        {
            int typeID = ComponentID<T>.Value;
            if (typeID >= components.Length)
            {
                Array.Resize(ref components, Math.Max(typeID + 1, components.Length * 2));
            }
            if (components[typeID] == null)
            {
                components[typeID] = new SparseSet<T>(MaxEntityCount);
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
            EntityQueryData<T,E,K> data = new( 
                ref GetOrCreateSet<T>().GetValue(entity),
                ref GetOrCreateSet<E>().GetValue(entity),
                ref GetOrCreateSet<K>().GetValue(entity) 
            );
            return data;
        }

        public void DestroyEntity(Entity entity)
        {
            Bitset bitset = entityMasks[entity.Id];

            for (int i = 0; i < 64; i++)
            {
                if (bitset.HasBit(i))
                {
                    components[i].Remove(entity);
                }
            }

            entityMasks[entity.Id].ClearBits();
            //TODO: Release ID back to EntityManager
        }

        public void RemoveComponent<T>(Entity entityId) where T : struct
        {
            SparseSet<T> set = GetOrCreateSet<T>();
            set.Remove(entityId);
        }


        public void Run()
        {
            foreach (var system in systems)
            {
                system.Run(this, ref commandBuffer);
            }

            commandBuffer.Flush(this);
        }

        private void AddToGroup(Entity entity)
        {
            Bitset bitset = entityMasks[entity.Id];
            if (!groups.TryGetValue(bitset, out var group))
            {
                group = new SparseSet<Entity>(MaxEntityCount);
                groups.Add(bitset, group);
            }
            group.Add(entity, entity);
        }

        public List<Entity> HasAll<T, E>() where T : struct where E : struct
        {
            ISparseSet s1 = GetOrCreateSet<T>();
            ISparseSet s2 = GetOrCreateSet<E>();
            var smallest = s1.Size < s2.Size ? s1 : s2;
            var other = s1.Size < s2.Size ? s2 : s1;

            List<Entity> entities = new List<Entity>(smallest.Size);

            var smallestEntities = smallest.GetEntities();
            for (int i = 0; i < smallestEntities.Count; i++)
            {
                if (other.Contains(smallestEntities[i]))
                {
                    entities.Add(smallestEntities[i]);
                }
            }
            return entities;
        }

        public List<Entity> HasAll<T, E, K>()
        {
            Bitset bitset = new Bitset();
            bitset.SetBit(componentBitRegistry.GetComponentBit<T>());
            bitset.SetBit(componentBitRegistry.GetComponentBit<E>());
            bitset.SetBit(componentBitRegistry.GetComponentBit<K>());
            return groups[bitset].GetDense();
        }

        public List<T> Has<T>(Entity entity) where T : struct
        {
            throw new NotImplementedException();
        }

        public Query<T> Query<T>()
        where T : struct
        {
            return new Query<T>(GetOrCreateSet<T>());
        }

        public Query<T, E> Query<T, E>()
        where T : struct
        where E : struct
        {
            return new Query<T, E>(GetOrCreateSet<T>(), GetOrCreateSet<E>());
        }

        public Query<T, E, K> Query<T, E, K>()
        where T : struct
        where E : struct
        where K : struct
        {
            return new Query<T, E, K>(GetOrCreateSet<T>(), GetOrCreateSet<E>(), GetOrCreateSet<K>(), entityMasks);
        }

        public List<T> GetComponentList<T>() where T : struct
        {
            return GetOrCreateSet<T>().GetDense();
        }

    }
}