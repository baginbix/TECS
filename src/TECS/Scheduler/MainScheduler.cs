using TECS.Commands;
using TECS.Executors;
using TECS.Resources;
using TECS.Scheduler.Labels;
using TECS.Systems;

namespace TECS.Scheduler
{
    public class MainScheduler : IResource, IScheduler
    {
        ECS _ecs;
        bool initialized = false;

        public MainScheduler(ECS ecs)
        {
            _ecs = ecs;
        }

        public void AddSystem<TSchedule>(SystemBinding system)
        {
            var schedulers = _ecs.GetResource<Schedulers>().GetResource();
            schedulers.schedulers[typeof(TSchedule)].AddSystem<TSchedule>(system);
        }

        public void RunPhase(ECS ecs)
        {
            var schedulers = ecs.GetResource<Schedulers>().GetResource();
            if (!initialized)
            {
                schedulers.schedulers[typeof(Startup)].RunPhase(ecs);
                initialized = true;
                ecs.Flush();
            }

            schedulers.schedulers[typeof(PreUpdate)].RunPhase(ecs);
            schedulers.schedulers[typeof(StateTransition)].RunPhase(ecs);
            ecs.Flush();
            schedulers.schedulers[typeof(Update)].RunPhase(ecs);
            var cmd = new CommandBuffer();
            var StateSchedule = (StateSchedule)schedulers.schedulers[typeof(StateTransition)];

            StateSchedule.RunUpdate(ecs, cmd);

            schedulers.schedulers[typeof(PostUpdate)].RunPhase(ecs);
            ecs.AddBuffer(cmd);
            ecs.Flush();
        }

        public void SetExecutor(IExecutor executor) { }
    }
}
