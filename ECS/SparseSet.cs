using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECS.ECS
{
    public class SparseSet<T>
    {
        List<T> dense = new List<T>();
        Dictionary<int,int> sparse = new Dictionary<int,int>();
        
    }
}