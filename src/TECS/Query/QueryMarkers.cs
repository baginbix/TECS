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


