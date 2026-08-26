using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using BenchmarkDotNet.Attributes;
using TECS;
using TECS.Commands;
using TECS.Query; // Ensure your Query namespace is included

namespace PerformanceTests;

public struct Position
{
    public float X;
    public float Y;
}

public struct Velocity
{
    public float Dx;
    public float Dy;
}

public record struct Component3(float Value1, float Value2, float Value3);

// --- Tag Components for Filtering ---
public struct IsPlayer { }

public struct IsFrozen { }

// --- Source-Generated Queries ---
[Query]
public ref struct Query1
{
    public ref Position p;
}

[Query]
public ref struct Query2
{
    public ref Position p;
    public ref Velocity v;
}

[Query]
public ref struct Query3
{
    public ref Position p;
    public ref Velocity v;
    public ref Component3 c;
}

[Query]
[With<IsPlayer>]
public ref struct QueryWith
{
    public ref Position p;
}

[Query]
[Without<IsFrozen>]
public ref struct QueryWithout
{
    public ref Position p;
}

[Query]
[With<IsPlayer>]
[Without<IsFrozen>]
public ref struct QueryWithAndWithout
{
    public ref Position p;
}

// --- Benchmark System ---
public static class BenchmarkSystems
{
    [System]
    public static void ProcessOneComponent(Query<Query1> query)
    {
        foreach (var item in query)
        {
            item.p.X += 1;
            item.p.Y += 1;
        }
    }
}

// This attribute tells BDN to track every single byte allocated
[MemoryDiagnoser]
[DisassemblyDiagnoser(printSource: true, maxDepth: 2)]
public class EcsBenchmarks
{
    private App app;
    private ECS ecs;
    private CommandBuffer cmd;

    [Params(1_000_000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Initialize the App, and grab its internal ECS for the other benchmarks
        app = new App();
        ecs = new ECS();
        cmd = new CommandBuffer();

        for (int i = 0; i < EntityCount; i++)
        {
            var e = cmd.SpawnEntity()
                .With(new Position { X = 0, Y = 0 })
                .With(new Velocity { Dx = 1f, Dy = 1f })
                .With(
                    new Component3
                    {
                        Value1 = 1f,
                        Value2 = 1f,
                        Value3 = 1f,
                    }
                );

            if (i % 2 == 0) // 50% of entities
            {
                cmd.InsertComponent(e, new IsPlayer());
            }

            if (i % 4 == 0) // 25% of entities
            {
                cmd.InsertComponent(e, new IsFrozen());
            }
        }

        cmd.Flush(ecs);

        // Register the benchmark system
        app.AddSystem(BenchmarkSystems.ProcessOneComponent);
    }

    [Benchmark]
    public void IterateSystemOneComponent()
    {
        // Benchmarks the entire App system runner pipeline!
        //app.Run();
        var query = new Query<Query1>(ecs, 0);
        foreach (var item in query)
        {
            item.p.X += 1;
            item.p.Y += 1;
        }
    }

    [Benchmark]
    public void IterateOneComponent()
    {
        var query = new Query<Query1>(ecs, 0);

        foreach (var item in query)
        {
            item.p.X += 1;
            item.p.Y += 1;
        }
    }

    [Benchmark]
    public void IterateTwoComponents()
    {
        var query = new Query<Query2>(ecs, 0);

        foreach (var item in query)
        {
            item.p.X += item.v.Dx;
            item.p.Y += item.v.Dy;
        }
    }

    [Benchmark]
    public void IterateThreeComponents()
    {
        var query = new Query<Query3>(ecs, 0);

        foreach (var item in query)
        {
            item.p.X += item.v.Dx * item.c.Value1;
            item.p.Y += item.v.Dy * item.c.Value2;
            item.p.X += item.v.Dx * item.c.Value3;
        }
    }

    [Benchmark]
    public void IterateWithFilter()
    {
        var query = new Query<QueryWith>(ecs, 0);

        foreach (var item in query)
        {
            item.p.X += 1;
            item.p.Y += 1;
        }
    }

    [Benchmark]
    public void IterateWithoutFilter()
    {
        var query = new Query<QueryWithout>(ecs, 0);

        foreach (var item in query)
        {
            item.p.X += 1;
            item.p.Y += 1;
        }
    }

    [Benchmark]
    public void IterateWithAndWithoutFilter()
    {
        var query = new Query<QueryWithAndWithout>(ecs, 0);

        foreach (var item in query)
        {
            item.p.X += 1;
            item.p.Y += 1;
        }
    }

    [Benchmark]
    public void IterateSparseSetAVX()
    {
        var positionSet = CollectionsMarshal.AsSpan(ecs.GetSparseSet<Position>().GetDense());
        var velocitySet = CollectionsMarshal.AsSpan(ecs.GetSparseSet<Velocity>().GetDense());

        var posSpan = MemoryMarshal.Cast<Position, float>(positionSet);
        var velSpan = MemoryMarshal.Cast<Velocity, float>(velocitySet);

        int i = 0;
        for (; i < posSpan.Length; i += 8)
        {
            Vector256<float> pVec = Vector256.LoadUnsafe(ref posSpan[i]);
            Vector256<float> vVec = Vector256.LoadUnsafe(ref velSpan[i]);

            Vector256<float> result = Vector256.Add(pVec, vVec);

            result.StoreUnsafe(ref posSpan[i]);
        }

        if (i < posSpan.Length)
        {
            for (; i < posSpan.Length; i += 2)
            {
                posSpan[i] += velSpan[i];
                posSpan[i + 1] += velSpan[i + 1];
            }
        }
    }
}
