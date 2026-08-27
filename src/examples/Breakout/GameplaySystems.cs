using TECS;
using TECS.Commands;
using TECS.Query;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using src.examples;
using static src.examples.HelperFunctions;
using TECS.Resources;

namespace Breakout;

public static class BreakoutSystems
{
    [System]
    public static void StartupSystem(CommandBuffer cmd, Res<Assets> assetsRes)
    {
        int blockRows = 5;
        int blockCols = 8;
        int blockWidth = 80;
        int blockHeight = 24;
        int spacing = 12;
        int startX = 40;
        int startY = 20;
        
        var assets = assetsRes.Value;
        var pixel = assets.GetTexture("pixel");

        for (int i = 0; i < blockRows; i++)
        {
            for (int j = 0; j < blockCols; j++)
            {
                var x = startX + (j * (blockWidth + spacing));
                var y = startY + (i * (blockHeight + spacing));

                cmd.SpawnEntity()
                    .With(new Position(new Vector2(x, y)))
                    .With(new Hitbox(blockWidth, blockHeight))
                    .With(new Sprite(pixel, Color.BlanchedAlmond, new Point(blockWidth, blockHeight)))
                    .With(new BlockMarker());
            }
        }

        // The Paddle
        cmd.SpawnEntity()
            .With(new PlayerMarker())
            .With(new Position(new Vector2(300, 400)))
            .With(new Hitbox(100, 20))
            .With(new Sprite(pixel, Color.Green, new Point(100, 20)));

        // Ball
        cmd.SpawnEntity()
            .With(new BallMarker())
            .With(new Position(new Vector2(350, 350)))
            .With(new Velocity(new Vector2(0, 1)))
            .With(new Hitbox(20, 20))
            .With(new Sprite(pixel, Color.Red, new Point(20, 20)));
    }

    [System]
    public static void MovePaddle(Query<PaddleQuery> query)
    {
        var paddle = query.Single();
        const int speed = 5;
        KeyboardState kstate = Keyboard.GetState();
        var velocity = new Vector2();
        
        if (kstate.IsKeyDown(Keys.A)) velocity.X = -speed;
        else if (kstate.IsKeyDown(Keys.D)) velocity.X = speed;
        
        // Because paddle is a ref struct returning refs, this mutates memory directly!
        paddle.pos.position += velocity;
    }

    [System]
    public static void MoveBall(Query<BallQuery> query)
    {
        var ball = query.Single();
        const float ballSpeed = 4;
        ball.pos.position += ball.vel.velocity * ballSpeed;

        // Check for window borders
        if (ball.pos.position.X <= 0)
        {
            ball.vel.velocity *= new Vector2(-1, 1);
            ball.pos.position = new Vector2(0, ball.pos.position.Y);
        }
        else if (ball.pos.position.X >= 780)
        {
            ball.vel.velocity *= new Vector2(-1, 1);
            ball.pos.position = new Vector2(780, ball.pos.position.Y);
        }

        if (ball.pos.position.Y <= 0)
        {
            ball.vel.velocity *= new Vector2(1, -1);
            ball.pos.position = new Vector2(ball.pos.position.X, 0);
        }

        Console.WriteLine($"Ball velocity: x:{Math.Abs(ball.vel.velocity.X)}, y:{Math.Abs(ball.vel.velocity.Y)}");
    }

    [System]
    public static void BallPaddleCollision(Query<BallQuery> ballQuery, Query<PaddleQuery> paddleQuery)
    {
        var ball = ballQuery.Single();
        var paddle = paddleQuery.Single();

        Rectangle ballRec = new(ball.pos.position.ToPoint(), new(ball.hitbox.width, ball.hitbox.heights));
        Rectangle paddleRec = new(paddle.pos.position.ToPoint(), new(paddle.hitbox.width, paddle.hitbox.heights));

        if (ballRec.Intersects(paddleRec))
        {
            var direction = (ballRec.Center - paddleRec.Center).ToVector2();
            direction.Normalize();
            ball.vel.velocity = direction;
        }
    }

    [System]
    public static void BallBrickCollision(Query<BallQuery> ballQuery, Query<BlockQuery> blocksQuery, CommandBuffer cmd)
    {
        var ball = ballQuery.Single();
        Rectangle ballRec = new(ball.pos.position.ToPoint(), new(ball.hitbox.width, ball.hitbox.heights));

        // Thanks to duck-typing on the ref struct enumerator, standard foreach just works!
        foreach (var block in blocksQuery)
        {
            Rectangle blockRec = new(block.pos.position.ToPoint(), new(block.hitbox.width, block.hitbox.heights));
            var collision = GetCollisionSide(ballRec, blockRec);
            
            if (collision != CollisionSide.None)
            {
                // 1. Destroy the brick using the extracted entity
                cmd.DestroyEntity(block.entity);

                // 2. Calculate the new velocity
                ball.vel.velocity = collision switch 
                {
                    CollisionSide.Top or CollisionSide.Bottom => new(ball.vel.velocity.X, -ball.vel.velocity.Y),
                    CollisionSide.Left or CollisionSide.Right => new(-ball.vel.velocity.X, ball.vel.velocity.Y),
                    _ => ball.vel.velocity 
                };
            }
        }
    }

    [System]
    public static void DrawSystem(Query<DrawQuery> query, Res<Graphics> graphicsRes)
    {
        var device = graphicsRes.Value;
        device.device.Clear(Color.Black);
        device.spriteBatch.Begin();
        
        foreach (var draw in query)
        {
            var rec = new Rectangle(draw.pos.position.ToPoint(), new Point(draw.hitbox.width, draw.hitbox.heights));
            device.spriteBatch.Draw(draw.sprite.texture, rec, draw.sprite.color);
        }

        device.spriteBatch.End();
    }
}