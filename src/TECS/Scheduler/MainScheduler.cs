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
            var schedulers = _ecs.GetResource<Schedulers>();
            schedulers.schedulers[typeof(TSchedule)].AddSystem<TSchedule>(system);
        }

        public void RunPhase(ECS ecs)
        {
            var schedulers = ecs.GetResource<Schedulers>();
            if (!initialized)
            {
                schedulers.schedulers[typeof(Startup)].RunPhase(ecs);
                initialized = true;
            }

            schedulers.schedulers[typeof(PreUpdate)].RunPhase(ecs);
            schedulers.schedulers[typeof(StateTransition)].RunPhase(ecs);
            schedulers.schedulers[typeof(Update)].RunPhase(ecs);

            schedulers.schedulers[typeof(PostUpdate)].RunPhase(ecs);
        }

        public void SetExecutor(IExecutor executor) { }
    }
}
