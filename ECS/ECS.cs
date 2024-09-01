using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECS.ECS
{
    public delegate void Run(ECS app);
    public class ECS
    {
        EntityManager entityManager;
        Dictionary<Type,object> components;

        List<Run> systems;

        public ECS(){
            components = new();
            entityManager = new();
            systems = new();
        }

        public Entity CreatEntity(){
            return entityManager.GetId();
        }

        public void AddSystem(Run system){
            systems.Add(system);
        }

        public void InsertComponent<T>(Entity entityId, T component){
            SparseSet<T> set = GetOrCreateSet<T>();
            set.Add(entityId,component);
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
    }
}