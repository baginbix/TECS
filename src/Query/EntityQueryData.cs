using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Elev.Documents.GitHub.ECS.src.Query
{
    public ref struct EntityQueryData<Comp1,Comp2,Comp3>
    {
        public ref Comp1 Component1;
        public ref Comp2 Component2;
        public ref Comp3 Component3; 

        public EntityQueryData(ref Comp1 c1, ref Comp2 c2, ref Comp3 c3) 
        {
            Component1 = ref c1;
            Component2 = ref c2;
            Component3 = ref c3;
        }
    }
}