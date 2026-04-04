using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TECS;
using TECS.Commands;

namespace TECS.Systems
{
    public enum SystemPhase
    {
        StartUp,         // Runs once when app is starting
        InitializeFrame, // Spawning, resetting frame data
        Input,           // Reading keyboard/mouse
        PreUpdate,       // AI decisions, pathfinding
        Update,          // General game logic (Default)
        Physics ,        // Movement, collision resolution
        PostUpdate,      // Camera tracking, cleanup
        Render,          // Drawing to the screen
        Count            // Magic trick: Gives us the exact size needed for the array
    }

    struct SystemItem
    {
        public ISystem System;
        public ulong LastRunTick;
    }
    internal class SystemManager
    {
        private readonly ECS ecs;
        private readonly List<SystemItem>[] systemGroups;
        
        private List<IStateManager> stateManagers ; 

        private CommandBuffer commandBuffer;

        public SystemManager(ECS ecs)
        {
            this.ecs = ecs;
            systemGroups = new List<SystemItem>[(int)SystemPhase.Count];
            for (int i = 0; i < systemGroups.Length; i++)
            {
                systemGroups[i] = new List<SystemItem>();
            }
            commandBuffer = new();
            stateManagers = new();
        }

        public void Add(SystemPhase phase, ISystem system)
        {
            systemGroups[(int)phase].Add(new SystemItem { System = system, LastRunTick = 0 });
        }

        public void AddStateManager(IStateManager manager)
        {
            stateManagers.Add(manager);
        }

        public void OnStart()
        {
            var startupPhaseGroup = systemGroups[0];
            foreach (var system in startupPhaseGroup)
            {
                ecs.SetActiveSystem(system.GetType());
                system.System.Run(ecs, commandBuffer);
            }
            
            commandBuffer.Flush(ecs);

            foreach (var sm in stateManagers)
            {
                sm.Initialize(ecs, ref commandBuffer);
            }
            commandBuffer.Flush(ecs);
        }
        
        public void UpdateSystems()
        {
            foreach (var sm in stateManagers)
            {
                sm.ProcessTransitions(ecs, ref commandBuffer);
            }
            
            
            int startPhase = (int)SystemPhase.InitializeFrame;
            for (int i = startPhase; i < systemGroups.Length; i++)
            {
                List<SystemItem> currentPhaseGroup = systemGroups[i];
                Span<SystemItem> span = CollectionsMarshal.AsSpan(currentPhaseGroup);
                for (int j = 0; j < span.Length; j++)
                {
                    ref SystemItem systemItem = ref span[j];
                    
                    ecs.SetActiveSystem(systemItem.System.GetType());
                    systemItem.System.Run(ecs, commandBuffer);
                    
                    systemItem.LastRunTick = ecs.GlobalTick; 
                }            
            }

            foreach (var sm in stateManagers)
            {
                sm.RunActiveState(ecs, ref commandBuffer);
            }
            
            commandBuffer.Flush(ecs);
        }
    }
}