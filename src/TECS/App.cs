using System.Diagnostics;
using TECS.Commands;
using TECS.Plugins;
using TECS.Query;
using TECS.Resources;
using TECS.Runner;
using TECS.Scheduler;
using TECS.Systems;

namespace TECS;

public delegate void SystemDelegate<T>(Query<T> query)
    where T : allows ref struct;

public abstract class SystemBinding
{
    public Type[] Reads { get; protected set; } = Array.Empty<Type>();
    public Type[] Writes { get; protected set; } = Array.Empty<Type>();
    public abstract void Run(ECS ecs, CommandBuffer cmd, uint lastRunTick);
}

public partial class App
{
    internal ECS ecs;

    //private SystemManager systemManager;

    private bool run = true;

    private bool Initialized = false;

    private IRunner _runner;
    private IScheduler _scheduler;
    public IScheduler Scheduler => _scheduler;

    public App()
    {
        ecs = new ECS();
        //systemManager = new (ecs);
        _runner = new RunnerOnce();
        // The main scheduler that runs the other schedulers
        var rootScheduler = new MainScheduler(ecs);
        _scheduler = rootScheduler;
        AddResource(rootScheduler);

        // Sub schedulers for each phase
        var schedulers = new Schedulers();
        AddResource(schedulers);
    }

    public App SetRunner(IRunner runner)
    {
        _runner = runner;
        return this;
    }

    public App SetRunner(Action<App> runner)
    {
        _runner = new LambdaRunner(runner);
        return this;
    }

    public App AddState<TState>(TState startState)
        where TState : struct, Enum
    {
        var stateManager = new StateManager<TState>(startState);
        //systemManager.AddStateManager(stateManager)
        ecs.InsertResource(stateManager);
        return this;
    }

    public App AddPlugin<TPlugin>()
        where TPlugin : IPlugin, new()
    {
        IPlugin plugin = new TPlugin();
        plugin.Build(this);
        return this;
    }

    public App AddPlugin(IPlugin plugin)
    {
        plugin.Build(this);
        return this;
    }

    public App AddSystemBinding(SystemBinding systemBinding, SystemPhase phase)
    {
        _scheduler.AddSystem(systemBinding, phase);
        //systemManager.Add(systemBinding,phase);
        return this;
    }

    public App AddSystemOnEnter<TState>(TState state, SystemBinding binding)
        where TState : struct, Enum
    {
        var stateManager = ecs.GetResource<StateManager<TState>>();
        stateManager.AddEnterSystem(state, binding);
        return this;
    }

    public App AddSystemOnUpdate<TState>(TState state, SystemBinding binding)
        where TState : struct, Enum
    {
        var stateManager = ecs.GetResource<StateManager<TState>>();
        stateManager.AddUpdateSystem(state, binding);
        return this;
    }

    public App AddSystemOnExit<TState>(TState state, SystemBinding binding)
        where TState : struct, Enum
    {
        var stateManager = ecs.GetResource<StateManager<TState>>();
        stateManager.AddExitSystem(state, binding);
        return this;
    }

    public App AddResource<TResource>(TResource resource)
        where TResource : IResource
    {
        ecs.InsertResource(resource);
        return this;
    }

    public App AddResource(IResource resource)
    {
        ecs.InsertResource(resource);
        return this;
    }

    public App SetScheduler(SystemPhase phase, IScheduler scheduler)
    {
        ecs.GetResource<Schedulers>().schedulers[phase] = scheduler;
        return this;
    }

    public void RunLoop()
    {
        if (Initialized)
        {
            throw new Exception("App is already running!");
        }
        Time.Time timeResource = new Time.Time();
        ecs.InsertResource(timeResource);

        //systemManager.OnStart();
        Initialized = true;
        Stopwatch stopwatch = Stopwatch.StartNew();
        long lastTick = stopwatch.ElapsedTicks;
        while (run)
        {
            long currentTick = stopwatch.ElapsedTicks;
            timeResource.DeltaTime = (float)(currentTick - lastTick) / Stopwatch.Frequency;
            timeResource.TotalTime = (float)stopwatch.Elapsed.TotalSeconds;
            lastTick = currentTick;

            Run();
        }
    }

    public void Run()
    {
        _runner.Run(this);
    }
}
