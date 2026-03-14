using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using TECS;
using TECS.Commands;

// This attribute tells BDN to track every single byte allocated
[MemoryDiagnoser] 
[DisassemblyDiagnoser(printSource: true, maxDepth: 2)]// Views the generated assembly code for the benchmarked methods.
public class EcsBenchmarks
{
    public struct Position { public float X; public float Y; }
    public struct Velocity { public float Dx; public float Dy; }


    public struct MoveSystem : IQueryAction<Position, Velocity>
    {
        public void Execute(ref Position p, ref Velocity v)
        {
            p.X += v.Dx;
            p.Y += v.Dy;
        }
    }

    public struct MoveSystemOneComponent : IQueryAction<Position>
    {
        public void Execute(ref Position p)
        {
            p.X += 1;
            p.Y += 1;
        }
    }

    private ECS ecs;
    private CommandBuffer cmd;




    [Params(1_000_000)]
    public int EntityCount { get; set; }


    [GlobalSetup]
    public void Setup()
    {
        ecs = new ECS(EntityCount);
        cmd = new CommandBuffer();

        for (int i = 0; i < EntityCount; i++)
        {
            var e = cmd.SpawnEntity();
            cmd.InsertComponent(new Position { X = 0, Y = 0 }, e);
            cmd.InsertComponent(new Velocity { Dx = 1f, Dy = 1f }, e);
        }
        
        cmd.Flush(ecs);
    }

    [Benchmark]
    public void IterateQueryLambda()
    {
        var moveQuery = ecs.Query<Position, Velocity>();
        moveQuery.ForEach((ref Position p, ref Velocity v) =>
        {
            p.X += v.Dx;
            p.Y += v.Dy;
        });
    }

    [Benchmark]
    public void IterateQueryLambdaOneComponent()
    {
        var moveQuery = ecs.Query<Position>();
        moveQuery.ForEach((ref Position p ) =>
        {
            p.X += 1;
            p.Y += 1;
        });
    }

    [Benchmark]
    public void IterateIActionOneComponent()
    {
        var moveQuery = ecs.Query<Position>();
        moveQuery.ForEach(new MoveSystemOneComponent());
    }

    [Benchmark]
    public void IterateQueryQueryAction()
    {
        var moveQuery = ecs.Query<Position, Velocity>();
        var action = new MoveSystem();
        moveQuery.ForEach(ref action);
    }

    [Benchmark]
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
