

using System;
using System.Buffers;
using System.Collections.Generic;

namespace TECS.Commands
{
    public class ComponentQueue<T> : ICommandQueue
    where T : struct
    {
        private List<(T component, Entity entity)> componentsToInsert;
        private List<Entity> componentsToRemove;
        public ComponentQueue()
        {
            componentsToInsert = new(100);
            componentsToRemove = new(100);
        }

        public void InsertComponent(T component, Entity entity)
        {
            componentsToInsert.Add((component, entity));
        }

        public void RemoveComponent(Entity entity)
        {
            componentsToRemove.Add(entity);
        }

        public void Flush(ECS ecs, Span<Entity > mappedIDs)
        {
            foreach(var (component, entity) in componentsToInsert)
            {
                var id = entity;
                if(id < 0){
                    id = mappedIDs[id * -1 - 1];
                }
                ecs.InsertComponent(id, component);
            }
            componentsToInsert.Clear();

            foreach(var entity in componentsToRemove){
                ecs.RemoveComponent<T>(entity);
            }
            componentsToRemove.Clear();
        }

    }
}