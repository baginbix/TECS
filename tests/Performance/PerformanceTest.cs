using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Diagnostics.Tracing.Parsers.MicrosoftAntimalwareEngine;
using TECS;
using TECS.Commands;

// This attribute tells BDN to track every single byte allocated
[MemoryDiagnoser] 
[DisassemblyDiagnoser(printSource: true, maxDepth: 2)]// Views the generated assembly code for the benchmarked methods.
/*
[HardwareCounters(
    BenchmarkDotNet.Diagnosers.HardwareCounter.CacheMisses, 
    BenchmarkDotNet.Diagnosers.HardwareCounter.BranchMispredictions, 
    BenchmarkDotNet.Diagnosers.HardwareCounter.BranchInstructions)]
*/
public class EcsBenchmarks

{
    public struct Position { public float X; public float Y; }
    public struct Velocity { public float Dx; public float Dy; }

    public record struct Component3 (float Value1,float Value2, float Value3);

    private ECS ecs;
    private CommandBuffer cmd;




    [Params(100_000)]
    public int EntityCount { get; set; }


    [GlobalSetup]
    public void Setup()
    {
        ecs = new ECS(EntityCount);
        cmd = new CommandBuffer();

        for (int i = 0; i < EntityCount; i++)
        {
            var e = cmd.SpawnEntity()
            .With(new Position { X = 0, Y = 0 })
            .With(new Velocity { Dx = 1f, Dy = 1f });

            Random rand = new Random();
            if(rand.NextDouble() < 2)
            {
                cmd.InsertComponent(new Component3 { Value1 = 1f, Value2 = 1f, Value3 = 1f }, e);
            }
        }
        
        cmd.Flush(ecs);
    }

    //[Benchmark]
    public void IterateQueryLambdaTwoComponents()
    {
        var moveQuery = ecs.Query<Position, Velocity>();
        moveQuery.ForEach((ref Position p, ref Velocity v) =>
        {
            p.X += v.Dx;
            p.Y += v.Dy;
        });
    }

    //[Benchmark]
    public void IterateQueryLambdaOneComponent()
    {
        var moveQuery = ecs.Query<Position>();
        moveQuery.ForEach((ref Position p ) =>
        {
            p.X += 1;
            p.Y += 1;
        });
    }

    //[Benchmark]
    public void IterateQueryLambdaTwoComponentsWithStruct()
    {
        var moveQuery = ecs.Query<Position, Velocity>();
        moveQuery.ForEach((ref Position p, ref Velocity v) =>
        {
            p.X += v.Dx;
            p.Y += v.Dy;
        });
    }

    //[Benchmark]
    public void IterateQueryLambdaThreeComponentsWithStruct()
    {
        var moveQuery = ecs.Query<Position, Velocity, Component3>();
        moveQuery.ForEach((ref Position p, ref Velocity v, ref Component3 c) =>
        {
            p.X += v.Dx * c.Value1;
            p.Y += v.Dy * c.Value2;
            p.X += v.Dx * c.Value3;
        });
    }

    



    [Benchmark]
    public void InteraterEnumeratorOneComponent()
    {
        var moveQuery = ecs.Query<Position>();  
        foreach(var pos in moveQuery)
        {
            pos.Write.X += 1;
            pos.Write.Y += 1;
        }
    }
    [Benchmark]
    public void InteraterEnumeratorTwoComponents()
    {
        var moveQuery = ecs.Query<Position, Velocity>();  
        foreach(var item in moveQuery)
        {
            item.Write<Position>().X += item.Read<Velocity>().Dx;
            item.Write<Position>().Y += item.Read<Velocity>().Dy;
        }
    }

     [Benchmark]
    public void InteraterEnumeratorThreeComponents()
    {
        var moveQuery = ecs.Query<Position, Velocity, Component3>();  
        foreach(var item in moveQuery)
        {
            item.Write<Position>().X += item.Read<Velocity>().Dx * item.Read<Component3>().Value1;
            item.Write<Position>().Y += item.Read<Velocity>().Dy * item.Read<Component3>().Value2;
            item.Write<Position>().X += item.Read<Velocity>().Dx * item.Read<Component3>().Value3;
        }
    }

    [Benchmark]
    public void InteraterEnumeratorThreeComponentsDeconstrution()
    {
        var moveQuery = ecs.Query<Position, Velocity, Component3>();  
        foreach((var pos, var vel, var comp3) in moveQuery)
        {
            pos.Write.X += vel.Read.Dx * comp3.Read.Value1;
            pos.Write.Y += vel.Read.Dy * comp3.Read.Value2;
            pos.Write.X += vel.Read.Dx * comp3.Read.Value3;
        }
    }

    //[Benchmark]
    public void IterateSparseSetAVX()
    {
        var positionSet = CollectionsMarshal.AsSpan( ecs.GetComponentList<Position>());
        var velocitySet = CollectionsMarshal.AsSpan( ecs.GetComponentList<Velocity>());

        var posSpan = MemoryMarshal.Cast<Position, float>(positionSet);
        var velSpan = MemoryMarshal.Cast<Velocity, float>(velocitySet);
        int i = 0;
        for (; i < posSpan.Length; i+=8)
        {
            Vector256<float> pVec = Vector256.LoadUnsafe(ref posSpan[i]);
            Vector256<float> vVec = Vector256.LoadUnsafe(ref velSpan[i]);

            Vector256<float> result = Vector256.Add(pVec, vVec);

            result.StoreUnsafe(ref posSpan[i]);
        
        }

        if(i < posSpan.Length)
        {
            for(; i < posSpan.Length; i+=2)
            {
                posSpan[i] += velSpan[i];
                posSpan[i+1] += velSpan[i+1];
            }
        }
    }

}
