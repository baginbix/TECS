using System;
using System.Net.Quic;
using TECS;
using TECS.Commands;




Console.WriteLine("Initializing TECS...");

// Create the ECS world
ECS ecs = new ECS(1_000_000);

// Register the system
ecs.AddSystem(new MovementSystem());

// Create Entity 1: A fast bullet
Entity bullet = ecs.CreateEntity();
ecs.InsertComponent(bullet, new Position { X = 0, Y = 0 });
ecs.InsertComponent(bullet, new Velocity { X = 10, Y = 0 }); // Moves 10 on the X axis
ecs.InsertComponent(bullet, new Health(100));

// Create Entity 2: A slow zombie
Entity zombie = ecs.CreateEntity();
ecs.InsertComponent(zombie, new Position { X = 5, Y = 5 });
ecs.InsertComponent(zombie, new Velocity { X = 0, Y = -1 }); // Moves -1 on the Y axis

// Create Entity 3: A rock (No velocity, shouldn't move!)
Entity rock = ecs.CreateEntity();
ecs.InsertComponent(rock, new Position { X = 100, Y = 100 });

// Simulate 3 frames of the game
for (int frame = 1; frame <= 3; frame++)
{
    Console.WriteLine($"\n=== Frame {frame} ===");
    ecs.Run(); 
}

Console.WriteLine("\nSimulation complete. Press any key to exit.");
Console.ReadLine();

// --- Define Components (Pure Data) ---
public record struct Position(float X, float Y); 

public record struct Velocity(float X, float Y);
public record struct Health(float Hp);

// --- Define Systems (Pure Logic) ---
public class MovementSystem : ISystem
{
    public void Run(ECS ecs, ref CommandBuffer cmd)
    {
        Console.WriteLine("--- Running Movement System ---");

        // Query for every entity with <Position, Velocity>
        var query = ecs.Query<Position, Velocity>();

        // Iterate over every entity with a lambda.
        query.ForEach(
            (Entity entity, ref Position pos, ref Velocity vel) =>
            {
                pos.X += vel.X;
                pos.Y += vel.Y;

                Console.WriteLine($"Entity {entity.Id} moved to {pos}");
            }
        );
    }
}

