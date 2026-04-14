using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Breakout;

public record struct Position(Vector2 position);
public struct PlayerMarker;
public record struct Hitbox(Rectangle bounds);
public record struct Velocity(Vector2 velocity);
public record struct Sprite(Texture2D texture, Color color, Point size);