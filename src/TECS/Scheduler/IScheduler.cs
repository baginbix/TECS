using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TECS.Commands;
using TECS.Executors;
using TECS.Systems;

namespace TECS.Scheduler
{
    /// <summary>
    /// IScheduler decides how systems are grouped in a phase.
    /// If you want multi-threading you might need to check read/writes of systems
    /// </summary>
    public interface IScheduler
    {
        void SetExecutor(IExecutor executor);
        void AddSystem(SystemBinding system, SystemPhase phase);

        void RunPhase(SystemPhase phase, ECS ecs);
    }
}