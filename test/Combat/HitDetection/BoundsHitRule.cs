using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace test.Combat.HitDetection;

public sealed class BoundsHitRule : IHitRule
{
    public bool Intersects(
        Texture2D texture,
        Color[] pixels,
        Rectangle target,
        Vector2 position,
        float rotation,
        SpriteEffects effects,
        Vector2 origin)
    {
        var cos = (float)System.Math.Cos(rotation);
        var sin = (float)System.Math.Sin(rotation);
        var left = float.MaxValue;
        var top = float.MaxValue;
        var right = float.MinValue;
        var bottom = float.MinValue;

        for (var y = 0; y <= 1; y++)
        {
            for (var x = 0; x <= 1; x++)
            {
                var localX = x * texture.Width - origin.X;
                var localY = y * texture.Height - origin.Y;
                if ((effects & SpriteEffects.FlipHorizontally) != 0)
                {
                    localX = texture.Width - x * texture.Width - origin.X;
                }

                var worldX = position.X + localX * cos - localY * sin;
                var worldY = position.Y + localX * sin + localY * cos;
                left = System.Math.Min(left, worldX);
                right = System.Math.Max(right, worldX);
                top = System.Math.Min(top, worldY);
                bottom = System.Math.Max(bottom, worldY);
            }
        }

        var bounds = new Rectangle(
            (int)System.Math.Floor(left),
            (int)System.Math.Floor(top),
            (int)System.Math.Ceiling(right - left),
            (int)System.Math.Ceiling(bottom - top));
        return bounds.Intersects(target);
    }
}
