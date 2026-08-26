using TECS.Commands;

namespace TECS.Executors
{
    public class MultiThreadExecutor : IExecutor
    {
        public void Execute(List<SystemNode> phaseNodes, ECS engine)
        {
            if (phaseNodes.Count == 0)
                return;

            var starters = new List<SystemNode>();
            foreach (var node in phaseNodes)
            {
                node.CurrentDependencyCount = node.InitialDependencyCount;
                if (node.InitialDependencyCount == 0)
                {
                    starters.Add(node);
                }
            }

            using var phaseBarrier = new CountdownEvent(phaseNodes.Count);
            List<CommandBuffer> buffers = new(10);
            foreach (var starter in starters)
            {
                CommandBuffer cmd = new();
                buffers.Add(cmd);
                DispatchNode(starter, engine, cmd, phaseBarrier);
            }

            phaseBarrier.Wait();

            foreach (var cmd in buffers)
                cmd.Flush(engine);
        }

        private void DispatchNode(
            SystemNode node,
            ECS engine,
            CommandBuffer cmd,
            CountdownEvent phaseBarrier
        )
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    node.System.System.Run(engine, cmd, node.System.LastRunTick);
                    node.System.LastRunTick = (uint)engine.GlobalTick;
                }
                finally
                {
                    foreach (var dependant in node.Dependents)
                    {
                        if (Interlocked.Decrement(ref dependant.CurrentDependencyCount) == 0)
                        {
                            DispatchNode(dependant, engine, cmd, phaseBarrier);
                        }
                    }
                    phaseBarrier.Signal();
                }
            });
        }
    }
}
