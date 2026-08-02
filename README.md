# TECS
**TECS** is a high-performance, data-oriented Entity-Component-System (ECS) for C#. Built from the ground up to leverage modern .NET features, it focuses on minimal memory allocations, cache-locality, and a clean, expressive API.

It utilizes a Sparse Set architecture to guarantee $O(1)$ component access while keeping data tightly packed for fast, cache-friendly iteration loops.

## Prerequisites
- .NET 8.0 or higher (Tested on .NET 10.0)

## Key Features
- **Zero-Allocation Queries:** Iterate through millions of entities without triggering the Garbage Collector.
- **Modern C#:** Takes full advantage of .NET 8+ features like `ref structs` and advanced pattern matching.
- **Expressive API:** Easy-to-read querying with built-in mutation tracking, and advanced filtering (`With<T>`, `Without<T>`).
- **Source generation:** Express yourself with easy to make queries and systems that utilize source generation to make the impossible possible.
- **Multi-threading:** To get maximum performance systems can be run in parallel.

## Plans
- Archetype support: TBA
- Customizable pipeline

## Limitations
- **No read-only query constraints**
  Due to C# generic limitations, all components in a `Query` are passed as `ref`. There is currently no built-in way to restrict a system to strictly read-only (`in`) access for specific components

- **Reference Semantics**
Due to C# generic constraints, components are passed by ref. Readonly is used to notify the system when a component will be just be read.


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
using TECS.Query;

// --- Components ---
// Simple structs hold your data.
public struct Position { public float X; public float Y; }
public struct Velocity { public float Dx; public float Dy; }

// Tag-components with no data for filtering
public struct PlayerTag { }
public struct Frozen { }

// --- Queries ---
// Queries are defined using source-generated ref structs!
// Use `ref readonly` for read-access and `ref` for write-access. 
// The engine's scheduler reads these to automatically multi-thread your systems!
[Query]
public ref struct MoveQuery
{
    public ref Position Pos;
    public ref readonly Velocity Vel;
}

// --- Systems ---
// Systems are just static methods tagged with [System]. 
// The source generator handles the binding, and dependencies (like CommandBuffer or Res<T>) are injected automatically.
public static class GameSystems 
{
    [System]
    public static void MoveSystem(Query<MoveQuery> movers) 
    {
        // Iterate through matching entities seamlessly
        foreach (var m in movers) 
        {
            m.Pos.X += m.Vel.Dx;
            m.Pos.Y += m.Vel.Dy;
        }
    }
}

// --- Application Setup & Loop ---
// The source generator automatically creates strongly-typed .AddSystem() extensions for your method!
App app = new App();

// Initialize entities directly in the ECS, or use a CommandBuffer inside your systems.
// Chaining .With() lets you insert components easily.
Entity player = app.Ecs.CreateEntity()
    .With(new Position { X = 0, Y = 0 })
    .With(new Velocity { Dx = 10, Dy = 5 })
    .With(new PlayerTag());

// Wire up your systems and run the game loop automatically.
app.AddSystem(GameSystems.MoveSystem, SystemPhase.Update)
   .RunLoop();

```

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
