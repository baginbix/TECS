using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TECS.Executors;
    
/// <summary>
/// Executes all the systems, it could be single-threaded execution or multi-threaded execution.
/// You can choose how multi-threaded execution works.
/// </summary>
public interface IExecutor{
    public void Execute(List<SystemNode> systems, ECS ecs);
}
