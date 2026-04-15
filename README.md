# TECS
**TECS** is a high-performance, data-oriented Entity-Component-System (ECS) for C#. Built from the ground up to leverage modern .NET features, it focuses on minimal memory allocations, cache-locality, and a clean, expressive API.

It utilizes a Sparse Set architecture to guarantee $O(1)$ component access while keeping data tightly packed for fast, cache-friendly iteration loops.

## Prerequisites
- .NET 8.0 or higher (Tested on .NET 10.0)

## Key Features
- **Zero-Allocation Queries:** Iterate through millions of entities without triggering the Garbage Collector.
- **Modern C#:** Takes full advantage of .NET 8+ features like `ref structs` and advanced pattern matching.
- **Expressive API:** Easy-to-read querying with built-in mutation tracking (`.Read` and `.Write`), and advanced filtering (`With<T>`, `Without<T>`).

## Plans
- Archetype support
- Source generation
- Multi-threading

## Limitations
- **No read-only query constraints**
  Due to C# generic limitations, all components in a `Query` are passed as `ref`. There is currently no built-in way to restrict a system to strictly read-only (`in`) access for specific components

- **Component Query Limit**
  The API currently supports querying up to 3 components simultaneously. While needing more than 3 often implies a system is doing too much and should be rethought, options to query 4+ components will be added.

- **Reference Semantics**
Due to C# generic constraints, components are passed by ref. The .Read and .Write API wrappers are implemented to explicitly show intent and track mutations.


## Installation
Currently, TECS is available as source code. A NuGet package is planned for the future. 

To use TECS in your project:
1. Clone or download this repository.
2. Copy the `src/TECS` folder directly into your C# project.
3. Make sure your project is targeting a modern .NET version (.NET 8.0+) to support `ref struct` features.

*(Alternatively, you can add this repository as a Git Submodule to keep up with changes!)*

## How to use

```csharp
using TECS;
using TECS.Commands;
// --- Components ---
public record struct Position(float X, float Y);
public record struct Velocity(float Dx, float Dy);

// Tag-components with no data for filtering
public record struct PlayerTag();
public record struct Frozen();


// --- Initialization & Entities ---
ECS ecs = new ECS();

Entity entity1 = ecs.CreateEntity();
ecs.InsertComponent(entity1, new Position(0, 0));
ecs.InsertComponent(entity1, new Velocity(10, 5));

// Chaining CreateEntity.With() lets you insert components easier. You can then save Entity in a variable
Entity entity2 = ecs.CreateEntity().With(new Position(0, 0))
.With(new Freeze());

// --- Querying ---
// You can iterate over components using deconstruction.
// Use .Read for read-only access, and .Write to mutate (which tracks changes automatically).
foreach (var (pos, vel) in ecs.Query<Position, Velocity>()) 
{
    pos.Write.X += vel.Read.Dx;
    pos.Write.Y += vel.Read.Dy;
}

// --- Advanced Filtering (With / Without) ---
// You can filter entities based on components they MUST or MUST NOT have, 
// For example: Move entities that have a Position and Velocity, as long as they aren't Frozen but are a Player!
foreach (var (pos, vel) in ecs.Query<Position, Velocity>().Without<Freeze>().With<PlayerTag>()) 
{
    pos.Write.X += vel.Read.Dx;
    pos.Write.Y += vel.Read.Dy;
}

// --- Systems ---
class MoveSystem : ISystem 
{
    public void Run(IEngine ecs, CommandBuffer cmd) 
    {
        foreach (var (pos, vel) in ecs.Query<Position, Velocity>())
        {
            pos.Write.X += vel.Read.Dx;
            pos.Write.Y += vel.Read.Dy;
        }
    }
}

// --- Application Loop ---
// You can use the App builder to easily wire up your systems and run the game loop automatically!

App app = new App()
    .AddSystem<MoveSystem>();
    
app.RunLoop();

```

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.