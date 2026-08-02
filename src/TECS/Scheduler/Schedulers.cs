using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TECS.Systems;

namespace TECS.Scheduler
{
    public class Schedulers:IResource
    {
        public readonly Dictionary<SystemPhase, IScheduler> schedulers = new(); 

        public Schedulers()
        {
            
            var phases = Enum.GetValues<SystemPhase>();
            for(int i = 0; i < phases.Length; i++)
            {
                var phase = phases[i];
                schedulers[phase] = new StandardSchedular();
            }
        }  
    }
}