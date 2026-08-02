using System;
using Microsoft.Xna.Framework;


namespace src.examples
{
    public static class HelperFunctions
    {
        public enum CollisionSide 
        {
            None,
            Top,
            Bottom,
            Left,
            Right
        }
        /// <summary>
        /// Determines which side of <paramref name="recB"/> is intersected by <paramref name="recA"/>.
        /// </summary>
        /// <param name="recA">The colliding rectangle (e.g., the ball).</param>
        /// <param name="recB">The target rectangle being hit (e.g., the brick).</param>
        /// <returns>
        /// The <see cref="CollisionSide"/> of <paramref name="recB"/> that was hit, 
        /// or <see cref="CollisionSide.None"/> if there is no intersection.
        /// </returns>
        /// <remarks>
        /// The returned side is relative to <paramref name="recB"/>. For example, if <paramref name="recA"/> 
        /// hits the left side of <paramref name="recB"/>, this returns <see cref="CollisionSide.Left"/>. 
        /// To get the side relative to <paramref name="recA"/>, invert the result.
        /// </remarks>
        public static CollisionSide GetCollisionSide(Rectangle recA, Rectangle recB)
        {
            // Checking for a collision before calculating which side
            if (!recA.Intersects(recB))
            {
                return CollisionSide.None;
            }

            float overlapLeft = recA.Right - recB.Left;
            float overlapRight = recB.Right - recA.Left;

            float overlapTop = recA.Bottom - recB.Top;

            float overlapBottom = recB.Bottom - recA.Top;

            float minOverlapX = Math.Min(overlapLeft, overlapRight);
            float minOverlapY = Math.Min(overlapTop,overlapBottom);

            if (minOverlapX < minOverlapY)
            {
                return overlapLeft < overlapRight ? CollisionSide.Left : CollisionSide.Right;
            }

            return overlapTop < overlapBottom ? CollisionSide.Top : CollisionSide.Bottom;
        }
    }
}