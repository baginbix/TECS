using TECS;
using TECS.Commands;
using Microsoft.Xna.Framework;

namespace Breakout;
public class StartupSystem:ISystem
{
    public void Run(IEngine engine, CommandBuffer cmd)
    {
        int blockRows = 5;
        int blockCols = 5;
        int blockWidth = 20;
        int blockHeight = 10;
        var assets = engine.GetResource<Assets>();
        var pixel = assets.GetTexture("pixel");

        for(int i = 0; i < blockRows; i++)
        {
            for (int j = 0; j < blockCols; j++)
            {
                cmd.SpawnEntity()
                .With(new Position(new Vector2(j*blockCols,i*blockRows)))
                .With(new Hitbox(blockWidth,blockHeight))
                .With(new Sprite(pixel, Color.BlanchedAlmond, new Point(blockWidth, blockHeight)));
            }
        }

        // The Paddle
        cmd.SpawnEntity()
        .With(new PlayerMarker())
        .With(new Position(new(300,400)))
        .With(new Hitbox(100,20))
        .With(new Sprite(pixel, Color.Green, new(100,20)));

        //Ball
        cmd.SpawnEntity()
        .With(new Position(new(350, 350)))
        .With(new Velocity(new(0,1)))
        .With(new Hitbox(20,20))
        .With(new Sprite(pixel, Color.Red, new(20,20)));
    }
}