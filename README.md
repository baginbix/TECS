# TECS
TECS is a performant ECS(Entity-Component-System) for C# created for education purposes, mainly for me.

It uses a SparseSet implementation with plans of adding Archetypes to use whichever suits your program.

**Note:** This project is a learning sandbox and is not currently meant for serious production use. I am building this to explore high-performance C# concepts, and I implement features that I find interesting or architecturally important rather than trying to build a fully-fledged commercial engine.

## Features
- Performant
- Minimal allocations
- Simple API

## Plans
- A guide for how to create a Entit-Component-System, skypjack made for [ENTT](https://skypjack.github.io/2019-02-14-ecs-baf-part-1/) but more in depth.
- Source generation
- Event system
- Multi-threading
- ForEach having the option to use Entity (Next in line to be implemented)

## How to use
```csharp
struct Position{ public float X, Y;}
struct Velocity{public float X, Y;}

var ecs = new ECS();
var entity = ecs.CreateEntity();
ecs.InsertComponent(entity, new Position { X = 0, Y = 0 });
ecs.InsertComponent(entity, new Velocity { X = 0, Y = 0 });

ecs.Query<Position>()
   .ForEach((ref p) => {
       p.X += 1;
   });

ecs.Query<Position, Velocity>()
    .ForEach((ref Position p, ref Velocity v) =>{
        p.X += v.X;
        p.Y += v.Y;
    });
```

