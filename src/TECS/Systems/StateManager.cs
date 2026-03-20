using TECS.Commands;

namespace TECS.Systems;

public class StateManager<TState> where TState: Enum
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
        
    }
}