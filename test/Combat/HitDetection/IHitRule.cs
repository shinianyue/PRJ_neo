using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace test.Combat.HitDetection;

public interface IHitRule
{
    bool Intersects(
        Texture2D texture,
        Color[] pixels,
        Rectangle target,
        Vector2 position,
        float rotation,
        SpriteEffects effects,
        Vector2 origin);
}
