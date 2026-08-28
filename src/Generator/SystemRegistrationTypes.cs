using System;

namespace Generator;

public abstract record SystemParam(string ParameterName, int Index)
{
    /// <summary>Full C# type name for delegates (e.g., global::TECS.Query.Query<global::Position>)</summary>
    public abstract string FullTypeName { get; }

    /// <summary>Clean type identifier used to generate unique class/delegate names</summary>
    public abstract string TypeSignatureId { get; }

    /// <summary>C# statement that runs before calling the system method (e.g., fetching events/resources)</summary>
    public virtual string? GenerateSetupCode() => null;

    /// <summary>Expression passed as an argument into the system call (e.g., writer_0 or new Query<...>(ecs))</summary>
    public abstract string GenerateArgumentCode();

    /// <summary>Code statement that adds read dependencies for the DAG</summary>
    public virtual string? GetReadsCode() => null;

    /// <summary>Code statement that adds write dependencies for the DAG</summary>
    public virtual string? GetWritesCode() => null;
}

public record QueryParam(string ParameterName, int Index, string StructType)
    : SystemParam(ParameterName, Index)
{
    public override string FullTypeName => $"global::TECS.Query.Query<{StructType}>";
    public override string TypeSignatureId =>
        "Query_" + StructType.Replace("global::", "").Replace(".", "_");

    public override string GenerateArgumentCode() =>
        $"new global::TECS.Query.Query<{StructType}>(ecs, systemTick)";

    public override string? GetReadsCode() =>
        $"readsList.AddRange({StructType}Extensions.GetReads);";

    public override string? GetWritesCode() =>
        $"writesList.AddRange({StructType}Extensions.GetWrites);";
}

public record CommandBufferParam(string ParameterName, int Index)
    : SystemParam(ParameterName, Index)
{
    public override string FullTypeName => "global::TECS.Commands.CommandBuffer";
    public override string TypeSignatureId => "CommandBuffer";

    public override string GenerateArgumentCode() => "cmd";
}

public record EventReaderParam(string ParameterName, int Index, string EventType)
    : SystemParam(ParameterName, Index)
{
    public override string FullTypeName => $"global::TECS.Event.EventReader<{EventType}>";
    public override string TypeSignatureId =>
        "EventReader_" + EventType.Replace("global::", "").Replace(".", "_");

    public override string? GenerateSetupCode() =>
        $"var reader_{Index} = ecs.GetEventReader<{EventType}>();";

    public override string GenerateArgumentCode() => $"reader_{Index}";

    public override string? GetReadsCode() => $"readsList.Add(typeof({EventType}));";
}

public record EventWriterParam(string ParameterName, int Index, string EventType)
    : SystemParam(ParameterName, Index)
{
    public override string FullTypeName => $"global::TECS.Event.EventWriter<{EventType}>";
    public override string TypeSignatureId =>
        "EventWriter_" + EventType.Replace("global::", "").Replace(".", "_");

    public override string? GenerateSetupCode() =>
        $"var writer_{Index} = ecs.GetEventWriter<{EventType}>();";

    public override string GenerateArgumentCode() => $"writer_{Index}";

    public override string? GetWritesCode() => $"writesList.Add(typeof({EventType}));";
}

public record ResParam(string ParameterName, int Index, string ResourceType)
    : SystemParam(ParameterName, Index)
{
    public override string FullTypeName => $"global::TECS.Resources.Res<{ResourceType}>";
    public override string TypeSignatureId =>
        "Res_" + ResourceType.Replace("global::", "").Replace(".", "_");

    public override string? GenerateSetupCode() =>
        $"var res_{Index} = new global::TECS.Resources.Res<{ResourceType}>(ref ecs.GetResource<{ResourceType}>());";

    public override string GenerateArgumentCode() => $"res_{Index}";

    public override string? GetReadsCode() => $"readsList.Add(typeof({ResourceType}));";
}

public record ResMutParam(string ParameterName, int Index, string ResourceType)
    : SystemParam(ParameterName, Index)
{
    public override string FullTypeName => $"global::TECS.Resources.ResMut<{ResourceType}>";
    public override string TypeSignatureId =>
        "ResMut_" + ResourceType.Replace("global::", "").Replace(".", "_");

    public override string? GenerateSetupCode() =>
        $"var resMut_{Index} = new global::TECS.Query.ResMut<{ResourceType}>(ref ecs.GetResource<ResourceType>());";

    public override string GenerateArgumentCode() => $"resMut_{Index}";

    public override string? GetReadsCode() => $"readsList.Add(typeof({ResourceType}));";

    public override string? GetWritesCode() => $"writesList.Add(typeof({ResourceType}));";
}
