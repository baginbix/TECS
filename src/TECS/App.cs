using System.Diagnostics;
using Microsoft.VisualBasic.CompilerServices;
using TECS.Plugins;
using TECS.Systems;
using TECS.Time;

namespace TECS;

public class App
{
    private ECS ecs;
    SystemManager systemManager;

    private bool run = true;

    public App(int maxEntitiesCount)
    {
        ecs = new ECS(maxEntitiesCount);
        systemManager = new (ecs);
    }

    public App AddState<TState>(TState startState) where TState : struct, Enum
    {
        var stateManger = new StateManager<TState>(startState);
        systemManager.AddStateManager(stateManger);
        ecs.InsertResource(stateManger);
        return this;
    }

    public App AddPlugin<TPlugin>() where TPlugin : IPlugin, new()
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

    /// <summary>
    /// Adds a system to a SystemPhase of your choice
    /// </summary>
    /// <param name="phase">The phase to put system into</param>
    public App AddSystem<TSystem>(SystemPhase phase) where TSystem: ISystem, new()
    {
        systemManager.Add(phase, new TSystem());
        return this;
    }
    
    /// <summary>
    /// Adds a system to SystemPhase.Update
    /// </summary>
    public App AddSystem<TSystem>() where TSystem : ISystem, new()
    {
        return AddSystem<TSystem>(SystemPhase.Update);
    }
    
    
    public App AddResource<TResource>(TResource resource) where TResource : IResource
    {
        ecs.InsertResource(resource);
        return this;
    }
    
    /// <summary>
    /// Adds a system to a SystemPhase of your choice
    /// </summary>
    /// <param name="phase">The phase to put system into</param>
    /// <param name="system">System to add</param>
    public App AddSystem(SystemPhase phase, ISystem system)
    {
        systemManager.Add(phase, system);
        return this;
    }
    
    /// <summary>
    /// Adds a system to SystemPhase.Update
    /// </summary>
    /// <param name="system">The system you want to add</param>
    public App AddSystem(ISystem system)
    {
        return AddSystem(SystemPhase.Update, system);
    }
    
    public App AddSystemOnEnter<TState>(TState state, ISystem system) 
        where TState : struct, Enum
    {
        var stateManager = ecs.GetResource<StateManager<TState>>();
        stateManager.AddEnterSystem(state, system);
        return this;
    }
    
    public App AddSystemOnUpdate<TState>(TState state, ISystem system) 
        where TState : struct, Enum
    {
        var stateManager = ecs.GetResource<StateManager<TState>>();
        stateManager.AddUpdateSystem(state, system);
        return this;
    }
    
    public App AddSystemOnExit<TState>(TState state, ISystem system) 
        where TState : struct, Enum
    {
        var stateManager = ecs.GetResource<StateManager<TState>>();
        stateManager.AddExitSystem(state, system);
        return this;
    }
    
    
    public App AddSystemOnEnter<TState, TSystem>(TState state) 
        where TState : struct, Enum
        where TSystem : ISystem, new()
    {
        var stateManager = ecs.GetResource<StateManager<TState>>();
        stateManager.AddEnterSystem(state, new TSystem());
        return this;
    }
    
    public App AddSystemOnUpdate<TState, TSystem>(TState state) 
        where TState : struct, Enum
        where TSystem : ISystem, new()
    {
        var stateManager = ecs.GetResource<StateManager<TState>>();
        stateManager.AddUpdateSystem(state, new TSystem());
        return this;
    }
    
    public App AddSystemOnExit<TState, TSystem>(TState state) 
        where TState : struct, Enum
        where TSystem : ISystem, new()
    {
        var stateManager = ecs.GetResource<StateManager<TState>>();
        stateManager.AddExitSystem(state, new TSystem());
        return this;
    }

    public App AddResource(IResource resource) 
    {
        ecs.InsertResource(resource);
        return this;
    }
    
    
    public void Run()
    {
        systemManager.OnStart();
        systemManager.UpdateSystems(); 
        ecs.Flush();
    }

    public void RunLoop()
    {
        Time.Time timeResource = new Time.Time();
        ecs.InsertResource(timeResource);
        
        systemManager.OnStart();

        Stopwatch stopwatch = Stopwatch.StartNew();
        long lastTick = stopwatch.ElapsedTicks;
        while (run)
        {
            long currentTick = stopwatch.ElapsedTicks;
            timeResource.DeltaTime = (float)(currentTick - lastTick);
            timeResource.TotalTime = (float)stopwatch.Elapsed.TotalSeconds;
            lastTick = currentTick;
            
            
            systemManager.UpdateSystems(); 
            ecs.Flush();
        }
    }

    public void Stop()
    {
        run = false;
    }


}