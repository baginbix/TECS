using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TECS.Scheduler;

namespace TECS.Runner
{
    /// <summary>
    /// This decides how a program runs once 
    /// App app = new App();
    /// app.Run() <- this runs the progam
    /// is called.
    /// Once a program is run, IRunner is used to decide exactly how it's run.
    /// A game will need a loop for the game to run, a console app might just need it to run sometimes
    /// </summary>
    public interface IRunner
    {   
        public void SetSchedular(IScheduler scheduler);
        public void Run(App app);
    }
}