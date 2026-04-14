using System;
using TECS;
using TECS.Commands;
using TECS.Systems;

Console.WriteLine("Initializing Event System Example...\n");

// 1. Setup the App using the new Builder pattern
App app = new App();

// 2. Register Systems using Phases
app.AddSystem<SetUp>(SystemPhase.StartUp);
app.AddSystem<PoisonSystem>(); // Defaults to SystemPhase.Update
app.AddSystem<DamageSystem>(); 

// 3. Simulate 2 Frames
for (int i = 1; i <= 2; i++)
{
    Console.WriteLine($"\n--- Frame {i} ---");
    app.Run();
}

Console.WriteLine("\nSimulation complete. Press any key to exit.");
Console.ReadLine();


// ==========================================
// --- Components & Events (Pure Data) ---
// ==========================================
public record struct Health(int Value);
public record struct Poison(Entity Target, int DamagePerTick);

// Events are just pure structs like components!
public record struct DamageEvent(Entity Target, int Amount);


// ==========================================
// --- Systems (Pure Logic) ---
// ==========================================

public class SetUp : ISystem
{
    public void Run(IEngine ecs, CommandBuffer cmd)
    {
        // 1. Spawn our Goblin using the fluent EntityBuilder
        Entity goblin = cmd.SpawnEntity()
            .With(new Health(50));
        cmd.InsertComponent(goblin, new Poison(goblin, 5));
    }
}


// System 1: The Publisher
// It only cares about generating events. It knows nothing about Health!
public class PoisonSystem : ISystem
{
    public void Run(IEngine ecs, CommandBuffer cmd)
    {
        // Grab the new EventWriter from the engine
        var writer = ecs.GetEventWriter<DamageEvent>();

        // Query over all poisoned entities using the new enumerator
        foreach (var item in ecs.Query<Poison>())
        {
            var poison = item.Read;
            
            // Fire off the event! 
            // (Note: If you named the method inside EventWriter 'Write' instead of 'SendEvent', adjust this accordingly!)
            writer.Send(new DamageEvent(poison.Target, poison.DamagePerTick)); 
            
            Console.WriteLine($"[PoisonSystem] Sent {poison.DamagePerTick} damage to Entity {poison.Target.Id}");
        }
    }
}


// System 2: The Subscriber
// It only cares about reading events. It knows nothing about Poison!
public class DamageSystem : ISystem
{
    public void Run(IEngine ecs, CommandBuffer cmd)
    {
        // Zero allocations! This returns a fast window into existing event memory for this frame.
        var events = ecs.ReadEvents<DamageEvent>();

        foreach (var evt in events.Read())
        {
            // Because IEngine might not expose GetComponent directly yet, 
            // we safely cast it to ECS to access the specific entity's components.
            if (ecs is ECS world)
            {
                var healthOpt = world.QueryComponent<Health>(evt.Target);

                // If the entity still exists and has Health, apply the damage
                if (healthOpt.IsSome)
                {
                    ref Health health = ref healthOpt.Unwrap();
                    health.Value -= evt.Amount;
                    
                    Console.WriteLine($"[DamageSystem] Entity {evt.Target.Id} took {evt.Amount} damage. Health: {health.Value}");
                }
            }
        }
    }
}