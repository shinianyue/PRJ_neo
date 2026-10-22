using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace test.Combat.HitDetection;

public sealed class PixelPerfectHitRule : IHitRule
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

        for (var y = 0; y < texture.Height; y++)
        {
            for (var x = 0; x < texture.Width; x++)
            {
                if (pixels[y * texture.Width + x].A == 0)
                {
                    continue;
                }

                var localX = x + 0.5f - origin.X;
                var localY = y + 0.5f - origin.Y;
                if ((effects & SpriteEffects.FlipHorizontally) != 0)
                {
                    localX = texture.Width - x - 0.5f - origin.X;
                }

                var worldX = position.X + localX * cos - localY * sin;
                var worldY = position.Y + localX * sin + localY * cos;
                if (target.Contains((int)System.Math.Floor(worldX), (int)System.Math.Floor(worldY)))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
