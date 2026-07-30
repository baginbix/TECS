using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TECS.Commands;
using TECS.Scheduler;

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
                _scheduler.RunPhase(Systems.SystemPhase.StartUp,     _app.Ecs);
                
            }
            _scheduler.RunPhase(Systems.SystemPhase.InitializeFrame, _app.Ecs);
            _scheduler.RunPhase(Systems.SystemPhase.Input,           _app.Ecs);
            _scheduler.RunPhase(Systems.SystemPhase.PreUpdate,       _app.Ecs);
            _scheduler.RunPhase(Systems.SystemPhase.Update,          _app.Ecs);
            _scheduler.RunPhase(Systems.SystemPhase.Physics,         _app.Ecs);
            _scheduler.RunPhase(Systems.SystemPhase.PostUpdate,      _app.Ecs);
            _scheduler.RunPhase(Systems.SystemPhase.Render,          _app.Ecs);   
            ecs.Flush();
            ecs.NextTick(); 
        }

        public void SetSchedular(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }
    }
}