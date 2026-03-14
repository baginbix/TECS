using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TECS.Components
{
    public static class ComponentRegistry
    {
        private static int _nextId = 0;
        private static List<Type> _types = new(100);
        public static int Next => _nextId++;

        public static Type GetType(int id) => _types[id];

        public static void Register(Type type)
        {
            if (!_types.Contains(type))
            {
                _types.Add(type);   
            }
        }
        
    }
}