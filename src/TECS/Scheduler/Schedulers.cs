using TECS.Resources;
using TECS.Scheduler.Labels;

namespace TECS.Scheduler
{
    public class Schedulers : IResource
    {
        public readonly Dictionary<Type, IScheduler> schedulers = new();

        public Schedulers()
        {
            schedulers[typeof(Startup)] = new StandardSchedular();
            schedulers[typeof(StateTransition)] = new StandardSchedular();
            schedulers[typeof(PreUpdate)] = new StandardSchedular();
            schedulers[typeof(Update)] = new StandardSchedular();
            schedulers[typeof(PostUpdate)] = new StandardSchedular();
        }
    }
}
