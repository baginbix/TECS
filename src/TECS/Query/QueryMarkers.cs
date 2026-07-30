using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TECS.Event;
using TECS;
using TECS.Commands;
using TECS.Queries;
using TECS.Query;

namespace TECS.Query;

public readonly ref struct Query<T> where T : allows ref struct
{
    public readonly ECS World;

    public Query(ECS world)
    {
        World = world;
    }

}

[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
public class WithAttribute<T> : Attribute where T: struct;

[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
public class WithoutAttribute<T> : Attribute where T: struct;

[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
public class ChangedAttribute<T> : Attribute where T: struct;

[AttributeUsage(AttributeTargets.Struct)]
public class QueryAttribute : Attribute;

public class MyRes:IResource;
public struct MyTest;
public ref struct MyQuery
{
    public ref MyTest t;
}

public static class Tests
{
    public static void F(Query<MyQuery> query,Res<MyRes> res)
    {
        
    }
}

