using TECS;
using TECS.Commands;
using TECS.Executors;
using TECS.Scheduler;
using TECS.Systems;

class StateSchedule : IScheduler
{
    private List<IStateManager> _states = new();

    public void AddSystem<TSchedule>(SystemBinding system) { }

    public void AddManager(IStateManager manager)
    {
        if (!_states.Contains(manager))
            _states.Add(manager);
    }

    public void RunPhase(ECS ecs)
    {
        var cmd = new CommandBuffer();
        foreach (var state in _states)
        {
            state.ProcessTransitions(ecs, ref cmd);
        }

        ecs.AddBuffer(cmd);
    }

    public void RunUpdate(ECS ecs, CommandBuffer cmd)
    {
        foreach (var state in _states)
        {
            state.RunActiveState(ecs, ref cmd);
        }
    }

    public void SetExecutor(IExecutor executor)
    {
        throw new NotImplementedException();
    }
}
