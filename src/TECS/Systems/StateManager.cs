using TECS.Commands;

namespace TECS.Systems;

public interface IStateManager
{
    void Initialize(ECS ecs, ref CommandBuffer cmd);
    void ProcessTransitions(ECS ecs, ref CommandBuffer cmd);
    void RunActiveState(ECS ecs, ref CommandBuffer cmd);

}

public class StateManager<TState> :IStateManager where TState: Enum
{
    private Stack<TState> activeStates = new();
    private TState? nextState;
    private bool isTransitioning;

    private Dictionary<TState, List<ISystem>> onEnter = new();
    private Dictionary<TState, List<ISystem>> onUpdate = new();
    private Dictionary<TState, List<ISystem>> onExit = new();
    
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
            sys.Run(ecs, ref cmd);
    }
    
    public TState CurrentState => activeStates.Peek();

    public void SetState(TState state)
    {
        nextState = state;
        isTransitioning = true;
    }
    
    public void AddEnterSystem(TState state, ISystem system) => onEnter[state].Add(system);
    public void AddUpdateSystem(TState state, ISystem system) => onUpdate[state].Add(system);
    public void AddExitSystem(TState state, ISystem system) => onExit[state].Add(system);

    public void ProcessTransitions(ECS ecs, ref CommandBuffer cmd)
    {
        if(!isTransitioning || nextState == null)
            return;

        TState oldState = CurrentState;
        TState newState = nextState;

        foreach(var system in onExit[oldState]) 
            system.Run(ecs, ref cmd);

        activeStates.Pop();
        activeStates.Push(newState);

        isTransitioning = false;
        nextState = default;

        foreach(var sys in onEnter[newState])
            sys.Run(ecs, ref cmd);
    }

    public void RunActiveState(ECS ecs, ref CommandBuffer cmd)
    {
        foreach (var sys in onUpdate[CurrentState])
        {
            sys.Run(ecs, ref cmd);
        }
    }
}