using System;
using System.Collections.Generic;

namespace TECS
{
    public class ECS
    {
        EntityManager entityManager;
        Dictionary<Type,ISparseSet> components;

        Dictionary<Bitset,SparseSet<Entity>> groups;

        ComponentBitRegistry componentBitRegistry;

        SparseSet<Bitset> entityMasks;

        List<ISystem> systems;

        Dictionary<Type,object> resources;

        public ECS(){
            components = new();
            entityManager = new();
            systems = new();
            groups = new();
            componentBitRegistry = new();
            entityMasks = new();
            resources = new();
        }

        public Entity CreateEntity(){
            Entity entity = entityManager.GetId();
            entityMasks.Add(entity,new Bitset());
            return entity;
        }

        public void AddSystem(ISystem system){
            systems.Add(system);
        }

        public void InsertResource<T>(T newResource){
            resources.Add(typeof(T),newResource);
        }

        public T GetResource<T>(){
            return (T)resources[typeof(T)];
        }

        public void InsertComponent<T>(Entity entityId, T component){
            SparseSet<T> set = GetOrCreateSet<T>();
            set.Add(entityId,component);
            var bitPosition = componentBitRegistry.GetComponentBit<T>();
            var bitset = entityMasks.GetValue(entityId);
            bitset.SetBit(bitPosition);
            entityMasks.Add(entityId,bitset);
            AddToGroup(entityId);
        }

        private SparseSet<T> GetOrCreateSet<T>(){
            if(!components.TryGetValue(typeof(T),out var set)){
                set = new SparseSet<T>();
                components.Add(typeof(T), set);
            }

            return (SparseSet<T>) set;
        }

        public List<T> QueryComponent<T>(){
            return ((SparseSet<T>)components[typeof(T)]).GetDense();
        }   

        public void DestroyEntity(Entity id) {
            Bitset bitset = entityMasks.GetValue(id);
            entityMasks.Remove(id);
            for(int i = 0; i < componentBitRegistry.size; i++){
                if((bitset.bits & (1ul << i)) != 0){
                    Type componentType = componentBitRegistry.GetComponentType(i);
                    components[componentType].Remove(id);
                }
            }
        }

        public void RemoveComponent<T>(Entity entityId) {
            SparseSet<T> set = GetOrCreateSet<T>();
            set.Remove(entityId);
        }


        public void Run(){
            foreach(var system in systems)
            {
                system.Run(this);
            }
        }

        private void AddToGroup(Entity entity){
            Bitset bitset = entityMasks.GetValue(entity);
            if(!groups.TryGetValue(bitset, out var group)){
                group = new SparseSet<Entity>();
                groups.Add(bitset, group);
            }
            group.Add(entity,entity);
        }

        public List<Entity> HasAll<T,E>(){
            ISparseSet s1 = (ISparseSet)components[typeof(T)];
            ISparseSet s2 = (ISparseSet)components[typeof(E)];
            var smallest = s1.Size < s2.Size ? s1 : s2;
            var other = s1.Size < s2.Size ? s2 : s1;

            List<Entity> entities = new List<Entity>(smallest.Size);

            var smallestEntities = smallest.GetEntities();
            for(int i = 0; i < smallestEntities.Count; i++)
            {
                if(other.Contains(smallestEntities[i])){
                    entities.Add(smallestEntities[i]);
                }
            }
            return entities;
        }

        public List<Entity> HasAll<T,E,K>(){
            Bitset bitset = new Bitset();
            bitset.SetBit(componentBitRegistry.GetComponentBit<T>());
            bitset.SetBit(componentBitRegistry.GetComponentBit<E>());
            bitset.SetBit(componentBitRegistry.GetComponentBit<K>());
            return groups[bitset].GetDense();
        }

        public List<T> Has<T>()
        {
            return ((SparseSet<T>)components[typeof(T)]).GetDense();
        }

        public Query<T> Query<T>()
        where T: struct
        {
            return new Query<T>((SparseSet<T>)components[typeof(T)]);
        }

        public Query<T,E> Query<T,E>()
        where T: struct 
        where  E: struct
        {
            return new Query<T,E>((SparseSet<T>)components[typeof(T)], (SparseSet<E>)components[typeof(E)]);
        }

    }
}