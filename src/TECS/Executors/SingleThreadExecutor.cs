using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TECS;
using TECS.Commands;

namespace TECS.Executors;

public class SingleThreadExecutor : IExecutor
{
    public void Execute(List<SystemNode> systems, ECS ecs)
    {
        CommandBuffer cmd = new();
        foreach (var system in systems)
        {
            system.System.System.Run(ecs, cmd, system.System.LastRunTick);
        }
        cmd.Flush(ecs);
    }
}
