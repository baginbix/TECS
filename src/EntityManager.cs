using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TECS
{
    //TODO: Implement entity recycling
    public class EntityManager
    {
        private int nextId = 0;

        public Entity GetId(){
            return nextId++;
        }
    }
}