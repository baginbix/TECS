using TECS;
using TECS.Plugins;
using TECS.Systems;

namespace Breakout;

public class BreakoutPlugin : IPlugin
{
    public void Build(App app)
    {
        app.AddSystem(BreakoutSystems.StartupSystem, SystemPhase.StartUp);
        app.AddSystem(BreakoutSystems.MovePaddle, SystemPhase.Input);
        app.AddSystem(BreakoutSystems.MoveBall, SystemPhase.Physics);
        app.AddSystem(BreakoutSystems.BallPaddleCollision, SystemPhase.Physics);
        app.AddSystem(BreakoutSystems.BallBrickCollision, SystemPhase.Physics);
        app.AddSystem(BreakoutSystems.DrawSystem, SystemPhase.Render);
    }
}