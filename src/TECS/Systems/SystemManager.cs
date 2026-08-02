using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TECS;
using TECS.Commands;
using TECS.Query;

namespace TECS.Systems;

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

    public struct SystemItem
    {
        public SystemBinding System;
        public ulong LastRunTick;
    }
    /*
    internal class SystemManager
    {
        private readonly ECS _ecs;
        private readonly List<SystemItem>[] _systemGroups;

        private readonly List<SystemNode>[] _systemGraphs;
        
        private List<IStateManager> _stateManagers ; 

        private CommandBuffer _commandBuffer;

        private SystemSchedular _scheduler = new();
        private SystemDispatcher _dispatcher = new();

        public SystemManager(ECS ecs)
        {
            this._ecs = ecs;
            _systemGroups = new List<SystemItem>[(int)SystemPhase.Count];
            for (int i = 0; i < _systemGroups.Length; i++)
            {
                _systemGroups[i] = new List<SystemItem>();
            }
            _commandBuffer = new();
            _stateManagers = new();
            _systemGraphs = new List<SystemNode>[(int)SystemPhase.Count];
            for (int i = 0; i < _systemGraphs.Length; i++)
            {
                _systemGraphs[i] = new List<SystemNode>();
            }
        }

        public void Add(SystemBinding binding, SystemPhase phase)
        {
            _systemGroups[(int)phase].Add(new SystemItem { System = binding, LastRunTick = 0 });
        }

        public void AddStateManager(IStateManager manager)
        {
            _stateManagers.Add(manager);
        }

        public void OnStart()
        {
            var startupPhaseGroup = _systemGroups[0];
            foreach (var system in startupPhaseGroup)
            {
                _ecs.SetActiveSystem(system.GetType());
                system.System.Run(_ecs,_commandBuffer);
            }
            
            _commandBuffer.Flush(_ecs);

            foreach (var sm in _stateManagers)
            {
                sm.Initialize(_ecs, ref _commandBuffer);
            }
            _commandBuffer.Flush(_ecs);
            uint systemCount = ((uint)SystemPhase.Count);
            for(uint i = 1; i < systemCount; i++)
            {
                _systemGraphs[i] = _scheduler.BuildGraph(_systemGroups[i]);
            }
        }
        
        public void UpdateSystems()
        {
            foreach (var sm in _stateManagers)
            {
                sm.ProcessTransitions(_ecs, ref _commandBuffer);
            }
            
            
            int startPhase = (int)SystemPhase.InitializeFrame;
            /*
            for (int i = startPhase; i < _systemGroups.Length; i++)
            {
                List<SystemItem> currentPhaseGroup = _systemGroups[i];
                Span<SystemItem> span = CollectionsMarshal.AsSpan(currentPhaseGroup);
                for (int j = 0; j < span.Length; j++)
                {
                    ref SystemItem systemItem = ref span[j];
                    
                    _ecs.SetActiveSystem(systemItem.System.GetType());
                    _ecs.SetLastSystemTick(systemItem.LastRunTick);
                    systemItem.System.Run(_ecs, _commandBuffer);
                    
                    systemItem.LastRunTick = _ecs.GlobalTick; 
                }            
            }
            
            for(int i = startPhase; i<_systemGraphs.Length-1; i++)
            {
                _dispatcher.ExecutePhase(_systemGraphs[i],_ecs,_commandBuffer);
                _commandBuffer.Flush(_ecs);
            }

            Span<SystemItem> span = CollectionsMarshal.AsSpan(_systemGroups[(int)SystemPhase.Render]);
            for (int j = 0; j < span.Length; j++)
            {
                ref SystemItem systemItem = ref span[j];
                
                _ecs.SetActiveSystem(systemItem.System.GetType());
                _ecs.SetLastSystemTick(systemItem.LastRunTick);
                systemItem.System.Run(_ecs,_commandBuffer);
                
                systemItem.LastRunTick = _ecs.GlobalTick; 
            }   
            foreach (var sm in _stateManagers)
            {
                sm.RunActiveState(_ecs, ref _commandBuffer);
            }
            
            _commandBuffer.Flush(_ecs);
        }
    }
}
*/