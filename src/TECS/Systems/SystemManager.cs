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
    