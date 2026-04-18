using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Breakout;

public record struct Position(Vector2 position);
public struct PlayerMarker;
public struct BallMarker;
public record struct Hitbox(int width, int heights);
public record struct Velocity(Vector2 velocity);
public record struct Sprite(Texture2D texture, Color color, Point size);