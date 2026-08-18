using TECS;
using TECS.Commands;
using TECS.Query;
using TECS.Systems;

Console.WriteLine("Initializing TECS...");

// 1. Create the main game application (the world that holds everything)
App ecs = new App();

// 2. Register our systems (the logic of the game)
// GameSystems.SetUp will run exactly once at the very beginning (StartUp phase)
ecs.AddSystem(GameSystems.SetUp, SystemPhase.StartUp);

// GameSystems.MovementSystem will run every single frame (default Update phase)
ecs.AddSystem(GameSystems.MovementSystem);

// 3. Run a simple game loop to simulate 3 frames of time
for (int frame = 1; frame <= 3; frame++)
{
    Console.WriteLine($"\n=== Frame {frame} ===");
    
    // ecs.Run() tells the engine to execute all registered systems for this frame
    ecs.Run(); 
}

Console.WriteLine("\nSimulation complete. Press any key to exit.");
Console.ReadLine();

// ==========================================
// --- COMPONENTS ---
// Components are just plain data. They hold stats, but have ZERO logic or methods.
// ==========================================
public struct Position { public float X; public float Y; }
public struct Velocity { public float X; public float Y; }
public struct Health { public float Hp; }


// ==========================================
// --- QUERIES ---
// Queries act like search filters. This struct tells the engine: 
// "Find me every entity in the game that has both a Position AND a Velocity."
// ==========================================
[Query]
public ref struct MovementQuery
{
    // By including the Entity type, we can know exactly which entity we are looking at
    public Entity Entity; 
    
    // 'ref' means we want to modify this data (Read & Write)
    public ref Position Pos;
    
    // 'ref readonly' means we only need to look at this data, not change it (Read-Only).
    // This makes the engine run faster!
    public ref readonly Velocity Vel; 
}


// ==========================================
// --- SYSTEMS ---
// Systems are where the actual game logic lives. They process the data.
// ==========================================
public static class GameSystems
{
    // The [System] attribute tells TECS to wire this method up automatically.
    [System]
    public static void SetUp(CommandBuffer cmd)
    {
        // A CommandBuffer lets us safely spawn entities and give them components.

        // Create Entity 1: A fast bullet
        Entity bullet = cmd.SpawnEntity();
        cmd.InsertComponent(bullet, new Position { X = 0, Y = 0 });
        cmd.InsertComponent(bullet, new Velocity { X = 10, Y = 0 }); // Moves right by 10
        cmd.InsertComponent(bullet, new Health { Hp = 100 });

        // Create Entity 2: A slow zombie (using a shorter, chained syntax)
        cmd.SpawnEntity()
           .With(new Position { X = 5, Y = 5 })
           .With(new Velocity { X = 0, Y = -1 }); // Moves down by 1

        // Create Entity 3: A rock 
        // Notice we give it a Position, but we DO NOT give it a Velocity component. 
        // Because of this, our MovementQuery will completely ignore the rock!
        Entity rock = cmd.SpawnEntity();
        cmd.InsertComponent(rock, new Position { X = 100, Y = 100 });
    }

    [System]
    public static void MovementSystem(Query<MovementQuery> query)
    {
        Console.WriteLine("--- Running Movement System ---");

        // The 'query' loop automatically provides only the entities that 
        // matched our filter (the bullet and the zombie, but not the rock).
        foreach (var item in query)
        {
            // Update the position by adding the velocity
            item.Pos.X += item.Vel.X;
            item.Pos.Y += item.Vel.Y;

            Console.WriteLine($"Entity {item.Entity.Id} moved to X: {item.Pos.X}, Y: {item.Pos.Y}");
        }
    }
}