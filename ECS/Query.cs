using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECS.ECS
{
    public class Query<T> where T : struct
    {
        public T[] query;
        public Query()
        {

        }
    }

    public class Query<T, E> 
    where T: struct 
    where  E: struct
    {
        
        public Query(List<T> c1, List<E> c2)
        {

        }   

        public void ForEach(Action<(ref T,ref E )> func)
        {
            
        }
    }
}