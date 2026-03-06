using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TECS
{
    public struct CommandBuffer
    {
        List<Entity> entitiesToBeDestroyed;

        public CommandBuffer()
        {
            entitiesToBeDestroyed = new(100);
        }

        public void DestroyEntity(Entity id){
            entitiesToBeDestroyed.Add(id);
        }

        public void Flush(ECS ecs){
            foreach(var entity in entitiesToBeDestroyed){
                ecs.DestroyEntity(entity);
            }
            entitiesToBeDestroyed.Clear();
        }
    }
}