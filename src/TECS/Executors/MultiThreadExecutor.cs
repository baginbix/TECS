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

            foreach (var starter in starters)
            {
                DispatchNode(starter, engine, phaseBarrier);
            }

            phaseBarrier.Wait();
        }

        private void DispatchNode(SystemNode node, ECS engine, CountdownEvent phaseBarrier)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var cmd = new CommandBuffer();

                try
                {
                    node.System.System.Run(engine, cmd, node.System.LastRunTick);
                    node.System.LastRunTick = engine.GlobalTick;
                }
                finally
                {
                    engine.AddBuffer(cmd);
                    foreach (var dependant in node.Dependents)
                    {
                        if (Interlocked.Decrement(ref dependant.CurrentDependencyCount) == 0)
                        {
                            DispatchNode(dependant, engine, phaseBarrier);
                        }
                    }
                    phaseBarrier.Signal();
                }
            });
        }
    }
}
