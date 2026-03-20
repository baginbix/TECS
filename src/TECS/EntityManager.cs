using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TECS
{
    internal class EntityManager
    {
        private int nextId = 0;

        List<int> freeIds = new();
        List<int> entityVersions = new();
        public Entity GetId(){
            Entity newEntity;
            int numEntitesDeleted = freeIds.Count;
            if(numEntitesDeleted > 0)
            {
                var id = freeIds[numEntitesDeleted-1];
                freeIds.RemoveAt(numEntitesDeleted-1);

                var version = entityVersions[id];
                
                newEntity = new(id,version);
            }
            else
            {
                newEntity = new Entity(nextId++,0);
                entityVersions.Add(0);
            }
            return newEntity ;
        }

        public void Free(Entity entity)
        {
            freeIds.Add(entity.Id);
            entityVersions[entity.Id]++;
        }

        public bool IsAlive(Entity entity)
        {
            return entityVersions[entity.Id] == entity.Version;
        }
    }
}