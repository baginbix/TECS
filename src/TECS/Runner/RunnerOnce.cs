using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TECS.Commands;
using TECS.Scheduler;
using TECS.Systems;

namespace TECS.Runner
{
    /// <summary>
    /// Runs the program once, then it's done.
    /// </summary>
    public class RunnerOnce: IRunner
    {
        private IScheduler _scheduler;
        bool initialized = false;
        private ECS ecs;
        public void Run(App _app)
        {
            if(!initialized)
            {
                ecs = _app.Ecs;
                ecs.InsertResource<Time.Time>();
                initialized = true; 
                _scheduler = ecs.GetResource<MainScheduler>();
            }
            _scheduler.RunPhase(SystemPhase.Update, ecs);
            ecs.Flush();
            ecs.NextTick(); 
        }

    }
}