using TECS;
using TECS.Executors;
using TECS.Plugins;
using TECS.Scheduler;
using TECS.Systems;

namespace Breakout;

public class BreakoutPlugin : IPlugin
{
    public void Build(App app)
    {
        var renderer = new StandardSchedular();
        renderer.SetExecutor(new SingleThreadExecutor());
        
        app
        .SetScheduler(SystemPhase.Render, renderer)
        .AddSystem(BreakoutSystems.StartupSystem, SystemPhase.StartUp)
        .AddSystem(BreakoutSystems.MovePaddle, SystemPhase.Input)
        .AddSystem(BreakoutSystems.MoveBall, SystemPhase.Physics)
        .AddSystem(BreakoutSystems.BallPaddleCollision, SystemPhase.Physics)
        .AddSystem(BreakoutSystems.BallBrickCollision, SystemPhase.Physics)
        .AddSystem(BreakoutSystems.DrawSystem, SystemPhase.Render);
    }
}