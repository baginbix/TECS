using TECS;
using TECS.Commands;
using TECS.Events;
using TECS.Query;

Console.WriteLine("Initializing Event System Example...\n");

App app = new App();

// StartUp runs once during setup. 
// Systems registered without a phase default to Update, running every frame when app.Run() is called.
app.AddSystem(GameSystems.SetUp, SystemPhase.StartUp);
app.AddSystem(GameSystems.PoisonSystem);
app.AddSystem(GameSystems.DamageSystem);

for (int i = 1; i <= 2; i++)
{
    Console.WriteLine($"\n--- Frame {i} ---");
    app.Run();
}

Console.WriteLine("\nSimulation complete. Press any key to exit.");
Console.ReadLine();


// Components and events are plain value types (structs).
// Keeping them pure data allows TECS to pack them tightly in contiguous memory arrays.
public struct Health { public int Value; }
public struct Poison { public Entity Target; public int DamagePerTick; }
public struct DamageEvent { public Entity Target; public int Amount; }


[Query]
public ref struct PoisonQuery
{
    public ref readonly Poison Poison;
}


public static class GameSystems
{
    // CommandBuffer defers structural changes (spawning, deleting, adding components) until safe points.
    // This prevents memory array reallocation while other systems are iterating over entities.
    [System]
    public static void SetUp(CommandBuffer cmd)
    {
        Entity goblin = cmd.SpawnEntity();
        cmd.InsertComponent(goblin, new Health { Value = 50 });
        cmd.InsertComponent(goblin, new Poison { Target = goblin, DamagePerTick = 5 });
    }

    // Emitting events decouples systems: PoisonSystem doesn't know or care if Health even exists.
    [System]
    public static void PoisonSystem(Query<PoisonQuery> query, EventWriter<DamageEvent> writer)
    {
        foreach (var item in query)
        {
            var poison = item.Poison;
            writer.Send(new DamageEvent { Target = poison.Target, Amount = poison.DamagePerTick });
            Console.WriteLine($"[PoisonSystem] Sent {poison.DamagePerTick} damage to Entity {poison.Target.Id}");
        }
    }

    // Events live in frame-bound buffers that clear automatically at frame end.
    // They live for a the entire frame after the frame it was sent.
    [System]
    public static void DamageSystem(EventReader<DamageEvent> events, ECS ecs)
    {
        foreach (var evt in events.Read())
        {
            var healthOpt = ecs.QueryComponent<Health>(evt.Target);

            if (healthOpt.IsSome)
            {
                ref Health health = ref healthOpt.Unwrap();
                health.Value -= evt.Amount;

                Console.WriteLine($"[DamageSystem] Entity {evt.Target.Id} took {evt.Amount} damage. Remaining Health: {health.Value}");
            }
        }
    }
}