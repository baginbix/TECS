using TECS.Resources;
using TECS.Scheduler.Labels;

namespace TECS.Scheduler
{
    public class Schedulers : IResource
    {
        public readonly Dictionary<Type, IScheduler> schedulers = new();

        public Schedulers()
        {
            schedulers[typeof(Startup)] = new StandardScheduler();
            schedulers[typeof(StateTransition)] = new StateSchedule();
            schedulers[typeof(PreUpdate)] = new StandardScheduler();
            schedulers[typeof(Update)] = new StandardScheduler();
            schedulers[typeof(PostUpdate)] = new StandardScheduler();
        }
    }
}
