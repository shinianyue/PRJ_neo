using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using test.Entities;

namespace test.World;

public sealed class Scene
{
    public Rectangle Bounds { get; }
    public Vector2 PlayerStart { get; }
    public IReadOnlyList<Rectangle> Platforms { get; }
    public IReadOnlyList<Rectangle> Obstacles { get; }
    public Enemy Enemy { get; }

    public Scene(
        Rectangle bounds,
        Vector2 playerStart,
        IReadOnlyList<Rectangle> platforms,
        IReadOnlyList<Rectangle> obstacles,
        Enemy enemy)
    {
        Bounds = bounds;
        PlayerStart = playerStart;
        Platforms = platforms;
        Obstacles = obstacles;
        Enemy = enemy;
    }

    public void Draw(
        SpriteBatch spriteBatch,
        Texture2D wallTexture,
        Texture2D obstacleTexture,
        Texture2D enemyTexture,
        Texture2D pixelTexture)
    {
        foreach (var platform in Platforms)
        {
            for (var y = platform.Top; y < platform.Bottom; y += wallTexture.Height)
            {
                for (var x = platform.Left; x < platform.Right; x += wallTexture.Width)
                {
                    var width = System.Math.Min(wallTexture.Width, platform.Right - x);
                    var height = System.Math.Min(wallTexture.Height, platform.Bottom - y);
                    spriteBatch.Draw(wallTexture, new Rectangle(x, y, width, height), Color.White);
                }
            }
        }

        foreach (var obstacle in Obstacles)
        {
            spriteBatch.Draw(obstacleTexture, obstacle, Color.White);
        }

        Enemy?.Draw(spriteBatch, enemyTexture, pixelTexture);
    }
}
