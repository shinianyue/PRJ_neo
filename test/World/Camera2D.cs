using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using test.Entities;

namespace test.World;

public sealed class Camera2D
{
    public Matrix Transform { get; private set; } = Matrix.Identity;

    public void Follow(Player player, Rectangle sceneBounds, Viewport viewport)
    {
        var maxX = System.Math.Max(sceneBounds.Left, sceneBounds.Right - viewport.Width);
        var cameraX = MathHelper.Clamp(
            player.Bounds.Center.X - viewport.Width / 2f,
            sceneBounds.Left,
            maxX);
        Transform = Matrix.CreateTranslation(-cameraX, 0f, 0f);
    }
}
