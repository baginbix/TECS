using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECS.ECS.Tests;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ECS.ECS
{
    public delegate void Run(ECS app);
    public class ECS
    {
        EntityManager entityManager;
        Dictionary<Type,object> components;

        Dictionary<Bitset,SparseSet<Entity>> groups;

        ComponentBitRegistry componentBitRegistry;

        SparseSet<Bitset> entityMasks;

        List<Run> systems;

        public ECS(){
            components = new();
            entityManager = new();
            systems = new();
            groups = new();
            componentBitRegistry = new();
            entityMasks = new();
        }

        public Entity CreatEntity(){
            Entity entity = entityManager.GetId();
            entityMasks.Add(entity,new Bitset());
            return entity;
        }

        public void AddSystem(Run system){
            systems.Add(system);
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

        public void DestroyEntity(int id) {

        }

        public void RemoveComponent<T>(Entity entityId) {
            SparseSet<T> set = GetOrCreateSet<T>();
            set.Remove(entityId);
        }


        public void Run(){
            foreach(var system in systems){
                system(this);
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

        public void HasAll<T,E>(){
        }
    }
}