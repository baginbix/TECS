using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TECS
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

        public Type GetComponentType(int bit)
        {
            return components.FirstOrDefault(x => x.Value == bit).Key;
        }

        private void InsertComponentBit(Type t){
            components[t] = nextBit++;
        }

        public int size => components.Count;
    }
}