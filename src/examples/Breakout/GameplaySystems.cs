
using TECS;
using TECS.Commands;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;

namespace Breakout;

public class MovePaddle : ISystem
{
    public void Run(IEngine engine, CommandBuffer cmd)
    {
        ref var paddlePos = ref engine.Query<Position>().With<PlayerMarker>().Single();

        KeyboardState kstate = Keyboard.GetState();

        if (kstate.IsKeyDown(Keys.A))
        {
            paddlePos.position.X--;
        }
        else if (kstate.IsKeyDown(Keys.D))
        {
            paddlePos.position.X++;
        }
    }
}

public class MoveBall: ISystem
{
    public void Run(IEngine engine, CommandBuffer cmd)
    {
        ref var ballPos = ref engine.Query<Position>().With<BallMarker>().Single();
        ref var ballVel = ref engine.Query<Velocity>().With<BallMarker>().SingleReadonly();
        const float ballSpeed = 2;
        ballPos.position *= ballVel.velocity * ballSpeed;
    }
}

public class BallPaddleCollision: ISystem
{
    public void Run(IEngine engine, CommandBuffer cmd)
    {
        ref var ballHitbox = ref engine.Query<Hitbox>().With<BallMarker>().SingleReadonly();
        ref var ballPos = ref engine.Query<Position>().With<BallMarker>().Single();
        Rectangle ballRec = new(ballPos.position.ToPoint(), new(ballHitbox.width, ballHitbox.heights));

        ref var paddleHitbox = ref engine.Query<Hitbox>().With<PlayerMarker>().SingleReadonly();
        ref var paddlePos = ref engine.Query<Position>().With<PlayerMarker>().Single();
        Rectangle paddleRec = new(paddlePos.position.ToPoint(), new(paddleHitbox.width, paddleHitbox.heights));

        if (ballRec.Intersects(paddleRec))
        {
            ref var ballVel = ref engine.Query<Velocity>().With<BallMarker>().Single();
            var direction = (ballRec.Center - paddleRec.Center).ToVector2();
            direction.Normalize();
            ballVel.velocity = direction;
        }
    }
}

public class DrawSystem: ISystem
{
    public void Run(IEngine engine, CommandBuffer cmd)
    {
        
    }
}