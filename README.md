# TECS
TECS is a performant ECS(Entity-Component-System) for C# created for education purposes, mainly for me.

It uses a SparseSet implementation with plans of adding Archetypes to use whichever suits your program.

**Note:** This project is a learning sandbox and is not currently meant for serious production use. I am building this to explore high-performance C# concepts, and I implement features that I find interesting or architecturally important rather than trying to build a fully-fledged commercial engine.

## Prerequisites
- .NET 8.0 or higher (Tested on .NET 10.0)

## Features
- Performant
- Minimal allocations
- Simple API

## Plans
- A guide for how to create an Entity-Component-System, like Skypjack made for [EnTT](https://skypjack.github.io/2019-02-14-ecs-baf-part-1/) but more in depth.
- Source generation
- Event system
- Multi-threading
- ForEach having the option to use Entity (Next in line to be implemented)
  
## Installation
There is no NuGet package for now but it might get added in the future. 

To use TECS in your project:
1. Clone or download this repository.
2. Copy the `src/TECS` folder directly into your C# project.
3. Make sure your project is targeting a modern .NET version (.NET 8.0+) to support `ref struct` features.

*(Alternatively, you can add this repository as a Git Submodule to keep up with changes!)*

## How to use

```csharp
using TECS;
using TECS.Commands;

struct Position { public float X, Y; }
struct Velocity { public float X, Y; }
struct Freeze { }

// --- Initialization ---
var ecs = new ECS();

var entity1 = ecs.CreateEntity();
ecs.InsertComponent(entity1, new Position { X = 0, Y = 0 });
ecs.InsertComponent(entity1, new Velocity { X = 0, Y = 0 });

var entity2 = ecs.CreateEntity();
ecs.InsertComponent(entity2, new Position { X = 0, Y = 0 });

// --- Querying ---
// You can use it like this:
ecs.Query<Position>()
   .ForEach((ref Position p) => {
       p.X += 1;
   });

ecs.Query<Position, Velocity>()
   .ForEach((ref Position p, ref Velocity v) => {
       p.X += v.X;
       p.Y += v.Y;
   });

// You can filter in case you want entities that don't have Velocity:
ecs.Query<Position>()
   .Without<Velocity>()
   .ForEach((ref Position p) => {
       p.X += 1;
   });

// You can filter in case you want entities that have Velocity, but don't need to use the component:
ecs.Query<Position>()
   .With<Velocity>()
   .ForEach((ref Position p) => {
       p.X += 1;
   });

// --- Advanced Filtering (With / Without) ---
// You can filter entities based on components they MUST or MUST NOT have, 
// without actually fetching that component's data into your loop.
//
// For example: Move entities that have Velocity, as long as they aren't Frozen!
ecs.Query<Position>()
   .With<Velocity>()
   .Without<Freeze>()
   .ForEach((ref Position p) => {
       p.X += 1;
   });

// --- Systems ---
class MoveSystem : ISystem {
    public void Execute(ECS ecs, ref CommandBuffer cmd) {
        ecs.Query<Position>()
           .ForEach((ref Position p) => {
               p.X += 1;
           });
    }
}

// Running the system
ecs.AddSystem(new MoveSystem());
ecs.Run();
```

## License
MIT