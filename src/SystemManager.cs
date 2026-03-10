using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TECS;
using TECS.Commands;

namespace src
{
    public class SystemManager
    {
        private readonly ECS ecs;
        private readonly List<ISystem> systems;

        private CommandBuffer commandBuffer;

        public SystemManager(ECS ecs)
        {
            this.ecs = ecs;
            systems = new();
            commandBuffer = new();
        }

        public SystemManager Add(ISystem system)
        {
            systems.Add(system);
            return this;
        }

        public SystemManager Add<ISys>() where ISys: ISystem, new()
        {
            systems.Add(new ISys());
            return this;
        }

        public void InitializeSystems()
        {
            foreach(var system in systems)
            {
                system.Initialize(ecs);
            }
        }

        public void UpdateSystems()
        {
            foreach(var system in systems)
            {
                system.Update(ecs, ref commandBuffer);
            }
        }

        public void TearDownSystems()
        {
            foreach(var system in systems)
            {
                system.TearDown(ecs);
            }
        }
    }
}