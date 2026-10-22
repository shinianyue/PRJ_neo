using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using test.Entities;
using test.Story;

namespace test.World;

public sealed class Scene
{
    public Rectangle Bounds { get; }
    public Vector2 PlayerStart { get; }
    public IReadOnlyList<Rectangle> Platforms { get; }
    public IReadOnlyList<Rectangle> Obstacles { get; }
    public Enemy Enemy { get; }
    public DialogueNpc Npc { get; }

    public Scene(
        Rectangle bounds,
        Vector2 playerStart,
        IReadOnlyList<Rectangle> platforms,
        IReadOnlyList<Rectangle> obstacles,
        Enemy enemy,
        DialogueNpc npc = null)
    {
        Bounds = bounds;
        PlayerStart = playerStart;
        Platforms = platforms;
        Obstacles = obstacles;
        Enemy = enemy;
        Npc = npc;
    }

    public void Draw(
        SpriteBatch spriteBatch,
        Texture2D wallTexture,
        Texture2D obstacleTexture,
        Texture2D enemyTexture,
        Texture2D weaponTexture,
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

        Enemy?.Draw(spriteBatch, enemyTexture, weaponTexture, pixelTexture);
        Npc?.Draw(spriteBatch, enemyTexture);
    }

    public void DrawBackground(SpriteBatch spriteBatch, Texture2D backgroundTexture)
    {
        var tileWidth = (int)System.Math.Round(
            Bounds.Height * (backgroundTexture.Width / (double)backgroundTexture.Height));
        for (var x = Bounds.Left; x < Bounds.Right; x += tileWidth)
        {
            var width = System.Math.Min(tileWidth, Bounds.Right - x);
            spriteBatch.Draw(
                backgroundTexture,
                new Rectangle(x, Bounds.Top, width, Bounds.Height),
                Color.White);
        }
    }
}
