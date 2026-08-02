using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TECS;
using TECS.Query; // Assuming this is where [Query] lives

namespace Breakout;

public record struct Position(Vector2 position);
public struct PlayerMarker;
public struct BallMarker;
public struct BlockMarker;
public record struct Hitbox(int width, int heights); // Maintained 'heights' from your original code
public record struct Velocity(Vector2 velocity);
public record struct Sprite(Texture2D texture, Color color, Point size);

// --- New Auto-Generated Queries ---

[Query]
[With<PlayerMarker>]
public ref struct PaddleQuery
{
    public ref Position pos;
    public ref Hitbox hitbox;
}

[Query]
[With<BallMarker>]
public ref struct BallQuery
{
    public ref Position pos;
    public ref Velocity vel;
    public ref Hitbox hitbox;
}

[Query]
[With<BlockMarker>]
public ref struct BlockQuery
{
    public Entity entity; // Explicitly requested so we can destroy blocks!
    public ref Position pos;
    public ref Hitbox hitbox;
}

[Query]
public ref struct DrawQuery
{
    public ref Position pos;
    public ref Hitbox hitbox;
    public ref Sprite sprite; // Added sprite so DrawSystem can fetch the color/texture
}

public class Graphics:IResource
{
    public SpriteBatch spriteBatch;
    public GraphicsDevice device;
}