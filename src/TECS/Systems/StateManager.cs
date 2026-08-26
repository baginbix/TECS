using TECS.Commands;
using TECS.Resources;

namespace TECS.Systems;

public interface IStateManager
{
    void Initialize(ECS ecs, ref CommandBuffer cmd);
    void ProcessTransitions(ECS ecs, ref CommandBuffer cmd);
    void RunActiveState(ECS ecs, ref CommandBuffer cmd);

}

public class StateManager<TState> :IStateManager,IResource where TState: struct, Enum
{
    private Stack<TState> activeStates = new();
    private TState? nextState;
    private bool isTransitioning;

    private Dictionary<TState, List<SystemBinding>> onEnter = new();
    private Dictionary<TState, List<SystemBinding>> onUpdate = new();
    private Dictionary<TState, List<SystemBinding>> onExit = new();
    
    public StateManager(TState initialState)
    {
        activeStates.Push(initialState);

        foreach (TState state in Enum.GetValues(typeof(TState)))
        {
            onEnter[state] = new();
            onUpdate[state] = new();
            onExit[state] = new();
        }
    }

    public void Initialize(ECS ecs, ref CommandBuffer cmd)
    {
        foreach(var sys in onEnter[CurrentState])
            sys.Run(ecs, cmd,0);
    }
    
    public TState CurrentState => activeStates.Peek();

    public void SetState(TState state)
    {
        nextState = state;
        isTransitioning = true;
    }
    
    //TODO::Fix StateManager it shouldn't need an object
    public void AddEnterSystem(TState state, SystemBinding system) => onEnter[state].Add(system);
    public void AddUpdateSystem(TState state, SystemBinding system) => onUpdate[state].Add(system);
    public void AddExitSystem(TState state, SystemBinding system) => onExit[state].Add(system);

    public void ProcessTransitions(ECS ecs, ref CommandBuffer cmd)
    {
        if(!isTransitioning || nextState == null)
            return;

        TState oldState = CurrentState;
        TState newState = nextState.Value;

        foreach(var system in onExit[oldState]) 
            system.Run(ecs, cmd, 0);

        activeStates.Pop();
        activeStates.Push(newState);

        isTransitioning = false;
        nextState = default;

        foreach(var sys in onEnter[newState])
            sys.Run(ecs, cmd, 0);
    }

    public void RunActiveState(ECS ecs, ref CommandBuffer cmd)
    {
        foreach (var sys in onUpdate[CurrentState])
        {
            sys.Run(ecs, cmd, 0);
        }
    }
}
