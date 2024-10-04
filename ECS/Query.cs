using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECS.ECS
{
    public class Query<T> where T : struct
    {
        public T[] query;
        public Query(){
            
        }
    }
}