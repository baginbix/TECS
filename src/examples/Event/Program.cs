using TECS;
using TECS.Commands;

Console.WriteLine("Initializing Event System Example...\n");

ECS ecs = new ECS(100);

// 1. Setup our World
Entity goblin = ecs.CreateEntity();
ecs.InsertComponent(goblin, new Health(50));
// Note: We store the Entity ID inside the component so the stateless system knows who to target!
ecs.InsertComponent(goblin, new Poison(goblin, 5)); 

// 2. Add our Stateless Systems
ecs.AddSystem(new PoisonSystem());
ecs.AddSystem(new DamageSystem());

// 3. Simulate 2 Frames
for (int i = 1; i <= 2; i++)
{
    Console.WriteLine($"\n--- Frame {i} ---");
    ecs.Run();
}

Console.WriteLine("\nSimulation complete. Press any key to exit.");
Console.ReadLine();


// ==========================================
// --- Components & Events (Pure Data) ---
// ==========================================
public record struct Health(int Value);
public record struct Poison(Entity Target, int DamagePerTick);
public record struct DamageEvent(Entity Target, int Amount);


// ==========================================
// --- Systems (Pure Logic) ---
// ==========================================

// System 1: The Publisher
// It only cares about generating events. It knows nothing about Health!
public class PoisonSystem : ISystem
{
    public void Run(ECS ecs, ref CommandBuffer cmd)
    {
        var poisons = ecs.GetComponentList<Poison>();

        foreach (var poison in poisons)
        {
            ecs.SendEvent(new DamageEvent(poison.Target, poison.DamagePerTick));
            Console.WriteLine($"[PoisonSystem] Sent {poison.DamagePerTick} damage to Entity {poison.Target.Id}");
        }
    }
}

// System 2: The Subscriber
// It only cares about reading events. It knows nothing about Poison!
public class DamageSystem : ISystem
{
    public void Run(ECS ecs, ref CommandBuffer cmd)
    {
        // Zero allocations! This just returns a fast window into existing memory.
        var events = ecs.ReadEvents<DamageEvent>();

        foreach (var evt in events)
        {
            var healthOpt = ecs.GetComponent<Health>(evt.Target);

            if (healthOpt.HasData)
            {
                ref Health health = ref healthOpt.Unwrap();
                health.Value -= evt.Amount;
                
                Console.WriteLine($"[DamageSystem] Entity {evt.Target.Id} took {evt.Amount} damage. Health: {health.Value}");
            }
        }
    }
}