using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator;

[Generator(LanguageNames.CSharp)]
public class QueryGenerator : IIncrementalGenerator
{
    // --- Data Models ---
    private record QueryField(string Type, string Name, bool IsRef, bool IsReadonly);

    private record QueryModel(
        string StructName,
        string NamespaceName,
        string entityFieldName,
        List<QueryField> Fields,
        List<string> WithTypes,
        List<string> WithoutTypes,
        List<string> ChangedTypes
    );

    // --- Initialization ---
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var structDeclarations = context
            .SyntaxProvider.CreateSyntaxProvider(
                predicate: static (s, _) => IsRefStruct(s),
                transform: static (ctx, _) => GetStructSemanticModel(ctx)
            )
            .Where(static m => m is not null)
            .Select(static (m, _) => m.Value);

        context.RegisterImplementationSourceOutput(
            structDeclarations,
            static (spc, source) => Execute(spc, source)
        );
    }

    private static bool IsRefStruct(SyntaxNode node)
    {
        if (node is StructDeclarationSyntax structDecl)
        {
            return structDecl.Modifiers.Any(m => m.ValueText == "ref");
        }
        return false;
    }

    private static bool IsReadonly(FieldDeclarationSyntax fiedlDecl)
    {
        return fiedlDecl.Modifiers.Any(m =>
            m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.ReadOnlyKeyword)
        );
    }

    static string GetNamespace(INamedTypeSymbol symbol)
    {
        // If the struct isn't wrapped in a namespace, it's in the global namespace.
        if (symbol.ContainingNamespace == null || symbol.ContainingNamespace.IsGlobalNamespace)
        {
            return string.Empty;
        }

        // Otherwise, return the full namespace (e.g., "UnitTestsECS.Queries")
        return symbol.ContainingNamespace.ToDisplayString();
    }

    private static (StructDeclarationSyntax Syntax, string Namespace)? GetStructSemanticModel(
        GeneratorSyntaxContext ctx
    )
    {
        var structDecl = (StructDeclarationSyntax)ctx.Node;
        var symbol = ctx.SemanticModel.GetDeclaredSymbol(structDecl) as INamedTypeSymbol;

        if (symbol == null)
            return null;

        return (structDecl, GetNamespace(symbol));
    }

    // --- Main Execution ---
    private static void Execute(
        SourceProductionContext context,
        (StructDeclarationSyntax structDecl, string Namespace) data
    )
    {
        var model = ParseQueryModel(data.structDecl, data.Namespace);
        if (model == null)
            return;

        string source = GenerateSourceCode(model);
        context.AddSource($"{model.StructName}Extensions.g.cs", source);
    }

    // --- Phase 1: Parsing ---
    private static QueryModel ParseQueryModel(StructDeclarationSyntax structDecl, string Namespace)
    {
        if (structDecl.TypeParameterList != null)
            return null;

        var withTypes = new List<string>();
        var withoutTypes = new List<string>();
        var changedTypes = new List<string>();
        bool isQuery = false;

        foreach (var attrList in structDecl.AttributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                if (attr.Name is IdentifierNameSyntax nameSyntax)
                {
                    if (nameSyntax.Identifier.Text is "Query")
                        isQuery = true;
                }
                else if (attr.Name is GenericNameSyntax genericName)
                {
                    string attrName = genericName.Identifier.Text;
                    string typeArg = genericName.TypeArgumentList.Arguments[0].ToString();

                    if (attrName == "With")
                        withTypes.Add(typeArg);
                    else if (attrName == "Without")
                        withoutTypes.Add(typeArg);
                    else if (attrName == "Changed")
                    {
                        changedTypes.Add(typeArg);
                    }
                }
            }
        }

        if (!isQuery)
            return null;
        string entityFieldName = null;
        var fields = new List<QueryField>();

        foreach (var f in structDecl.Members.OfType<FieldDeclarationSyntax>())
        {
            string rawType = f.Declaration.Type.ToString();
            string cleanType = rawType.Replace("ref", "").Replace("readonly", "").Trim();
            string fieldName = f.Declaration.Variables.First().Identifier.Text;
            bool isReadonly = IsReadonly(f);
            // If it's the Entity field, save the name but DO NOT add to fields list
            if (cleanType == "Entity")
            {
                entityFieldName = fieldName;
            }
            else
            {
                fields.Add(
                    new QueryField(
                        Type: cleanType,
                        Name: fieldName,
                        IsRef: rawType.Contains("ref"),
                        IsReadonly: isReadonly
                    )
                );
            }
        }

        if (fields.Count == 0)
            return null;

        return new QueryModel(
            structDecl.Identifier.Text,
            Namespace,
            entityFieldName,
            fields,
            withTypes,
            withoutTypes,
            changedTypes
        );
    }

    // --- Phase 2: Generation ---
    private static string GenerateSourceCode(QueryModel model)
    {
        var fieldsSB = new StringBuilder();
        var constructorsSB = new StringBuilder();
        var moveNextChecks = new StringBuilder();

        GenerateComponentCaches(model, fieldsSB, constructorsSB);
        GenerateFilterCaches(model, fieldsSB, constructorsSB, moveNextChecks);

        string smallestSetLogic = GenerateSmallestSetOptimization(model);
        string moveNextLogic = GenerateMoveNextLogic(model, moveNextChecks.ToString());
        string fieldAssignments = GenerateCurrentAssignments(model);

        string namespaceStart = string.IsNullOrEmpty(model.NamespaceName)
            ? ""
            : $"namespace {model.NamespaceName}\n{{";
        string namespaceEnd = string.IsNullOrEmpty(model.NamespaceName) ? "" : "}";
        bool needsEntity = model.Fields.Count > 1 || !string.IsNullOrEmpty(model.entityFieldName);
        string entityLocal = needsEntity
            ? "Entity entity = Unsafe.Add(ref _entities, _index);"
            : "";

        string singleMethod = GenerateSingleMethod(model);
        string readAccess = GenerateAccess(FieldAccess.Read, model);
        string writeAccess = GenerateAccess(FieldAccess.Write, model);
        return $$"""
            // <auto-generated/>
            using System;
            using System.Runtime.InteropServices;
            using System.Runtime.CompilerServices;
            using TECS;
            using TECS.Query;
            using TECS.Resources;

            {{namespaceStart}}

                public static class {{model.StructName}}Extensions
                {
                    {{readAccess}}
                    {{writeAccess}}
                    public static {{model.StructName}}Enumerator GetEnumerator(this Query<{{model.StructName}}> query)
                    {
                        return new {{model.StructName}}Enumerator(query.World, query.SystemTick);
                    }

                    {{singleMethod}}
                }



                public ref struct {{model.StructName}}Enumerator
                {
                    private readonly ECS _ecs;
                    private ref Entity _entities;
            {{fieldsSB}}
                    private readonly int _denseLength;
                    private int _index;
                    private int _indexDriver;
                    private readonly uint _systemTick;

                    public {{model.StructName}}Enumerator(ECS ecs, uint systemTick)
                    {
                        _ecs = ecs;
                        _index = -1;
                        _systemTick = systemTick;

            {{constructorsSB}}
            {{smallestSetLogic}}
                    }

                    public bool MoveNext()
                    {
            {{moveNextLogic}}
                        return false;
                    }

                    public {{model.StructName}} Current
                    {
                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
                        get
                        {
                            {{entityLocal}}
                            return new {{model.StructName}}
                            {
            {{fieldAssignments}}
                            };
                        }
                    }

                    public Entity CurrentEntity{
                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
                        get{
                            return Unsafe.Add(ref _entities, _index);
                        }
                    }
                }

            {{namespaceEnd}}
            """;
    }

    // --- Generation Helpers ---
    private static void GenerateComponentCaches(
        QueryModel model,
        StringBuilder fields,
        StringBuilder constructors
    )
    {
        bool needTick = false;
        for (int i = 0; i < model.Fields.Count; i++)
        {
            var field = model.Fields[i];
            fields.AppendLine($"        private ref {field.Type} _dense{i};");
            fields.AppendLine($"        private ref int[] _sparse{i};");
            fields.AppendLine($"        internal int _idx{i};");

            constructors.AppendLine($"            var set{i} = _ecs.GetSparseSet<{field.Type}>();");
            constructors.AppendLine(
                $"            _dense{i} = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(set{i}.GetDense()));"
            );
            constructors.AppendLine(
                $"            _sparse{i} = ref MemoryMarshal.GetReference(set{i}.GetSparseSet());"
            );
            bool isChanged = model.ChangedTypes.Contains(field.Type);
            if (!field.IsReadonly || isChanged)
            {
                fields.AppendLine($"    private ref uint _ticks{i};");
                constructors.AppendLine(
                    $"            _ticks{i} = ref MemoryMarshal.GetReference(set{i}.GetLastTicks());"
                );
                needTick = true;
            }
        }
        fields.AppendLine(needTick ? $"        private uint _currentTick;" : "");
        constructors.AppendLine(needTick ? "        _currentTick = (uint)_ecs.GlobalTick;" : "");
    }

    private static void GenerateFilterCaches(
        QueryModel model,
        StringBuilder fields,
        StringBuilder constructors,
        StringBuilder checks
    )
    {
        for (int i = 0; i < model.WithTypes.Count; i++)
        {
            string type = model.WithTypes[i];
            fields.AppendLine($"        private readonly SparseSet<{type}> _with{i};");
            constructors.AppendLine($"            _with{i} = _ecs.GetSparseSet<{type}>();");
            checks.AppendLine($"                        if (!_with{i}.Contains(entity)) continue;");
        }

        for (int i = 0; i < model.WithoutTypes.Count; i++)
        {
            string type = model.WithoutTypes[i];
            fields.AppendLine($"        private readonly SparseSet<{type}> _without{i};");
            constructors.AppendLine($"            _without{i} = _ecs.GetSparseSet<{type}>();");
            checks.AppendLine(
                $"                        if (_without{i}.Contains(entity)) continue;"
            );
        }
    }

    private static string GenerateSmallestSetOptimization(QueryModel model)
    {
        var sb = new StringBuilder();
        if (model.Fields.Count > 1)
        {
            for (int i = 0; i < model.Fields.Count - 1; i++)
            {
                sb.Append(i == 0 ? "            if(" : "            else if(");
                for (int j = i; j < model.Fields.Count; j++)
                {
                    if (i == j)
                        continue;
                    sb.Append($"set{i}.Size < set{j}.Size");
                    if (j < model.Fields.Count - 1)
                        sb.Append(" && ");
                }
                sb.AppendLine(")");
                sb.AppendLine("            {");
                sb.AppendLine($"                _indexDriver = {i};");
                sb.AppendLine(
                    $"                _entities  = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(set{i}.GetEntities()));"
                );
                sb.AppendLine($"                _denseLength = set{i}.Size;");
                sb.AppendLine("            }");
            }
            sb.AppendLine("            else");
            sb.AppendLine("            {");
            sb.AppendLine($"                _indexDriver = {model.Fields.Count - 1};");
            sb.AppendLine(
                $"                _entities = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(set{model.Fields.Count - 1}.GetEntities()));"
            );
            sb.AppendLine($"                _denseLength = set{model.Fields.Count - 1}.Size;");
            sb.AppendLine("            }");
        }
        else
        {
            sb.AppendLine(
                $"            _entities = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(set0.GetEntities()));"
            );
            sb.AppendLine($"            _denseLength = set0.Size;");
        }
        return sb.ToString();
    }

    private static string GenerateSingleMethod(QueryModel model)
    {
        var idx = new StringBuilder();
        var getSets = new StringBuilder();
        var objectInit = new StringBuilder();

        if (!string.IsNullOrEmpty(model.entityFieldName))
        {
            objectInit.AppendLine($" {model.entityFieldName} = entity,");
        }
        for (int i = 0; i < model.Fields.Count; i++)
        {
            idx.AppendLine($"int idx{i} = enumerator._idx{i};");
            getSets.AppendLine(
                $"            var set{i} = _ecs.GetSparseSet<{model.Fields[i].Type}>();"
            );
            getSets.AppendLine(
                $"            ref var dense{i} = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(set{i}.GetDense()));"
            );

            string comma = (i == model.Fields.Count - 1) ? "" : ",";
            string refStr = model.Fields[i].IsRef ? "ref" : "";
            objectInit.AppendLine(
                $" {model.Fields[i].Name} = {refStr} Unsafe.Add(ref dense{i}, idx{i}){comma}"
            );
        }

        bool needsEntity = model.Fields.Count > 1 || !string.IsNullOrEmpty(model.entityFieldName);
        string entity = needsEntity ? "Entity entity = enumerator.CurrentEntity;" : "";

        string method = $$"""
                public static {{model.StructName}} Single(this Query<{{model.StructName}}> query)
                {
                    var enumerator = query.GetEnumerator();
                    bool hasFirst = enumerator.MoveNext();
                        #if DEBUG
                        if (!hasFirst)
                        {
                            throw new InvalidCastException($"No {{model.StructName}} has been added.");
                        }
                        #endif
                        {{entity}}
                        {{idx}}
                        #if DEBUG
                        if (enumerator.MoveNext())
                        {
                            throw new InvalidOperationException($"More than one entity with queried component {{model.StructName}} has been found!");
                        }
                        #endif

                        var _ecs = query.World;

                        {{getSets}}
                        return new {{model.StructName}}{
                            {{objectInit}}
                        };
                }

            """;

        return method;
    }

    private static string GenerateMoveNextLogic(QueryModel model, string filterChecks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("            switch (_indexDriver) {");

        for (int driver = 0; driver < model.Fields.Count; driver++)
        {
            sb.AppendLine($"                case {driver}:");
            sb.AppendLine($"                    while(++_index < _denseLength) {{");
            sb.AppendLine(
                $"                        Entity entity = Unsafe.Add(ref _entities, _index);"
            );
            sb.AppendLine($"                        int entityId = entity.Id;");

            if (model.Fields.Count > 1)
            {
                sb.AppendLine(
                    $"                        int pageIndex = entityId >> 12; // Page shift"
                );
                sb.AppendLine(
                    $"                        int pageOffset = entityId & 4095; // Page mask"
                );
            }

            sb.Append(filterChecks);
            if (model.ChangedTypes.Contains(model.Fields[driver].Type))
            {
                sb.AppendLine(
                    $"              if (Unsafe.Add(ref _ticks{driver}, _index) <= _systemTick) continue;"
                );
            }

            for (int other = 0; other < model.Fields.Count; other++)
            {
                if (driver == other)
                    continue;

                sb.AppendLine(
                    $$"""
                      if(pageIndex >= _sparse{{other}}.Length) continue;
                      
                      int[] page{{other}} = Unsafe.Add(ref _sparse{{other}}, pageIndex);
                      if(page{{other}} == null) continue;
                      
                      ref int pageData{{other}} = ref MemoryMarshal.GetArrayDataReference(page{{other}});
                      _idx{{other}} = Unsafe.Add(ref pageData{{other}}, pageOffset);

                      if(_idx{{other}} < 0) continue;
                      _idx{{driver}} = _index;
                    """
                );

                if (model.ChangedTypes.Contains(model.Fields[other].Type))
                {
                    sb.AppendLine(
                        $"              if (Unsafe.Add(ref _ticks{other}, _idx{other}) <= _systemTick) continue;"
                    );
                }

                /*
                    sb.AppendLine(
                    $"                        if(pageIndex >= _sparse{other}.Length) continue;"
                );
                sb.AppendLine(
                    $"                        int[] page{other} = Unsafe.Add(ref _sparse{other}, pageIndex);"
                );
                sb.AppendLine($"                        if(page{other} == null) continue;");
                sb.AppendLine();
                sb.AppendLine(
                    $"                        ref int pageData{other} = ref MemoryMarshal.GetArrayDataReference(page{other});"
                );
                sb.AppendLine(
                    $"                        _idx{other} = Unsafe.Add(ref pageData{other}, pageOffset);"
                );
                */
            }
            sb.AppendLine($"                        _idx{driver} = _index;");
            for (int i = 0; i < model.Fields.Count; i++)
            {
                if (!model.Fields[i].IsReadonly)
                {
                    sb.AppendLine(
                        $"                        Unsafe.Add(ref _ticks{i}, _idx{i}) = _currentTick;"
                    );
                }
            }
            sb.AppendLine($"                        return true;");
            sb.AppendLine($"                    }}");
            sb.AppendLine($"                    break;");
        }
        sb.AppendLine("            }");
        return sb.ToString();
    }

    private static string GenerateCurrentAssignments(QueryModel model)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(model.entityFieldName))
        {
            sb.AppendLine($"                    {model.entityFieldName} = entity,");
        }
        for (int i = 0; i < model.Fields.Count; i++)
        {
            var field = model.Fields[i];
            string comma = (i == model.Fields.Count - 1) ? "" : ",";
            string refStr = field.IsRef ? "ref" : "";
            sb.AppendLine(
                $"                    {field.Name} = {refStr} Unsafe.Add(ref _dense{i}, _idx{i}){comma}"
            );
        }
        return sb.ToString();
    }

    private static string GenerateAccess(FieldAccess fieldAccess, QueryModel model)
    {
        bool readOnly = false;
        string access = "Writes";
        if (fieldAccess == FieldAccess.Read)
        {
            access = "Reads";
            readOnly = true;
        }

        return $$"""
            public static Type[] Get{{access}} = new Type[]{ 
                        {{string.Join(
                ", ",
                model.Fields.Where(t => t.IsReadonly == readOnly).Select(t => $"typeof({t.Type})")
            )}} 
                    };
            """;
    }

    private enum FieldAccess
    {
        Read = 1,
        Write = 2,
        ReadWrite = 3,
    }
}
