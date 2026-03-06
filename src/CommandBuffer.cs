using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TECS
{
    public struct CommandBuffer
    {
        List<Entity> entitiesToBeDestroyed;

        List<Entity> entitiesToSpawn;
        int currentFakeID = -1;
        public CommandBuffer()
        {
            entitiesToBeDestroyed = new(100);
            entitiesToSpawn = new(100);
        }

        private Entity GenerateID(){
            return currentFakeID--;
        }

        public void DestroyEntity(Entity id){
            entitiesToBeDestroyed.Add(id);
        }

        public Entity SpawnEntity()
        {
            var entity = GenerateID();
            entitiesToSpawn.Add(entity);
            return entity;
        }

        public void Flush(ECS ecs){
            foreach(var entity in entitiesToBeDestroyed){
                ecs.DestroyEntity(entity);
            }
            entitiesToBeDestroyed.Clear();
        }


    }
}