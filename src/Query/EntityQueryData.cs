using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace TECS.Query;
public ref struct EntityQueryData<Comp1,Comp2,Comp3>
{
    public bool HasData;
    public ref Comp1 Component1;
    public ref Comp2 Component2;
    public ref Comp3 Component3; 

    public EntityQueryData(ref Comp1 c1, ref Comp2 c2, ref Comp3 c3) 
    {
        Component1 = ref c1;
        Component2 = ref c2;
        Component3 = ref c3;
        HasData = true;
    }

    public EntityQueryData()
    {
        Component1 = ref Unsafe.NullRef<Comp1>();
        Component2 = ref Unsafe.NullRef<Comp2>();
        Component3 = ref Unsafe.NullRef<Comp3>();
        HasData = false;
    }

    public static EntityQueryData<Comp1,Comp2,Comp3> None => new EntityQueryData<Comp1, Comp2, Comp3>();
}