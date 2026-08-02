using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TECS.Executors;
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
        public void AddSystem(SystemBinding system, SystemPhase phase)
        {
            var schedulers = _ecs.GetResource<Schedulers>();
            schedulers.schedulers[phase].AddSystem(system, phase);
        }

        public void RunPhase(SystemPhase phase, ECS ecs)
        {
            var schedulers = ecs.GetResource<Schedulers>();
            if(!initialized)
            {
                schedulers.schedulers[SystemPhase.StartUp].RunPhase(SystemPhase.StartUp,ecs);
                initialized = true;
            }
            
            var phases = Enum.GetValues<SystemPhase>();
            for(int i = 1; i < phases.Length; i++)
            {
                var p = phases[i];
                schedulers.schedulers[p].RunPhase(p, ecs);
            }
        }

        public void SetExecutor(IExecutor executor)
        {
        }
    }
}