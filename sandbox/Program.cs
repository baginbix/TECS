using TECS;
using TECS.Query;
using TECS.Systems; // Make sure we have access to SystemPhase and your generated extensions!
using Sandbox;
using TECS.Commands;
using TECS.Scheduler;      // Import the namespace we are about to create

// Top-level execution statement
App app = new App();
app.SetRunner( runner =>
{
    IScheduler scheduler = runner.Scheduler;
    for(int i =0; i < 3; i++)
    {
                
            scheduler.RunPhase(SystemPhase.Update, app.Ecs);
    }
});
app.AddSystem(GameSystems.Setup, SystemPhase.StartUp);
app.AddSystem(GameSystems.SystemA);
app.Run();

// 1. Put everything inside a specific namespace to prevent generator crashes!
namespace Sandbox
{
    public static class GameSystems
    {
        [System]
        public static void Setup(CommandBuffer cmd)
        {
            var e = cmd.SpawnEntity();
            cmd.InsertComponent(e,new A{a = 42});
        }
        [System]
        public static void SystemA(Query<AQeury> a)
        {
            Console.WriteLine($"Hello world nr:{a.Single().a.a}");
        }
    }

    public struct A
    {
        public int a;
    }

    [Query]
    public ref struct AQeury
    {
        public ref A a;
    }
}