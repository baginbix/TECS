using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECS.ECS.Tests
{
    public class ComponentBitRegistry
    {
        Dictionary<Type,int> components = new Dictionary<Type,int>();
        int nextBit = 0;

        public int GetComponentBit<T>(){
            var type = typeof(T);
            if(!components.ContainsKey(type)){
                InsertComponentBit(type);
            }

            return components[type];
        }

        private void InsertComponentBit(Type t){
            components[t] = nextBit++;
        }
    }
}