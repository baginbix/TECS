using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TECS.Commands;
using TECS.Executors;
using TECS.Systems;

namespace TECS.Scheduler
{
    public class StandardSchedular : IScheduler
    {
        private IExecutor _executor;
        private readonly List<SystemItem> _systems = new();
        private bool _isDirty = true;
        private List<SystemNode> _graph = new();

        public StandardSchedular()
        {
            _executor = new MultiThreadExecutor();
        }

        public void SetExecutor(IExecutor executor) => _executor = executor;

        public void AddSystem<T>(SystemBinding system)
        {
            _systems.Add(new() { System = system, LastRunTick = 0 });
            _isDirty = true;
        }

        public void RunPhase(ECS ecs)
        {
            if (_isDirty)
            {
                _graph = BuildGraph(_systems);
                _isDirty = false;
            }
            _executor.Execute(_graph, ecs);
        }

        private List<SystemNode> BuildGraph(List<SystemItem> systems)
        {
            var nodes = new List<SystemNode>();
            foreach (var sys in systems)
            {
                nodes.Add(new SystemNode(sys));
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                SystemNode current = nodes[i];

                for (int j = 0; j < i; j++)
                {
                    SystemNode previous = nodes[j];

                    //TODO: Since I reworked how Queries and Systems are created and added
                    // I need to add back Read/Write for my systems
                    if (HasDependency(current.System.System, previous.System.System))
                    {
                        current.InitialDependencyCount++;

                        previous.Dependents.Add(current);
                    }
                }
            }

            return nodes;
        }

        private bool HasDependency(SystemBinding current, SystemBinding previous)
        {
            bool writeOverlap =
                current.Writes.Intersect(previous.Reads).Any()
                || current.Writes.Intersect(previous.Writes).Any();

            bool readOverlaps = current.Reads.Intersect(previous.Writes).Any();

            return writeOverlap || readOverlaps;
        }
    }
}
