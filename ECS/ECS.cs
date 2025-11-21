using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ECS.ECS.Tests;
using ECS.ECS.Tests.Components;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ECS.ECS
{
    public delegate void Run(ref TestComponents.Position p,ref TestComponents.Velocity v);
    public class ECS
    {
        EntityManager entityManager;
        Dictionary<Type,object> components;

        Dictionary<Bitset,SparseSet<Entity>> groups;

        ComponentBitRegistry componentBitRegistry;

        SparseSet<Bitset> entityMasks;

        List<Run> systems;

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

        public void AddSystem(Run system){
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

        public void DestroyEntity(int id) {

        }

        public void RemoveComponent<T>(Entity entityId) {
            SparseSet<T> set = GetOrCreateSet<T>();
            set.Remove(entityId);
        }


        public void Run(){
            foreach(var system in systems){
                var components = HasAll<TestComponents.Position,TestComponents.Velocity>();
                for(int i = 0; i < components.Count; i++){
                    var component1 = components[i].Item1;
                    var component2 = components[i].Item2;
                    system(ref component1,ref component2);
                    GetOrCreateSet<TestComponents.Position>().Add(components[i].Item3,component1);
                    GetOrCreateSet<TestComponents.Velocity>().Add(components[i].Item3,component2);
                }
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
            Bitset bitset = new Bitset();
            bitset.SetBit(componentBitRegistry.GetComponentBit<T>());
            bitset.SetBit(componentBitRegistry.GetComponentBit<E>());
            return groups[bitset].GetDense();
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

        public Query<T,E> Query<T, E>()
        {
                        Query<T, E> query = new Query<T, E>();
            Bitset bitset = new Bitset();
            var componentBitT = componentBitRegistry.GetComponentBit<T>();
            var t = components[typeof(T)].GetDense();
            bitset.SetBit(componentBitT);
            bitset.SetBit(componentBitRegistry.GetComponentBit<E>());
            

            var TE = groups[bitset].GetDense();
        }

    }
}