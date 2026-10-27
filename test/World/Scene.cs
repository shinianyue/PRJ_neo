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
    public IReadOnlyList<Rectangle> Landmarks { get; }
    public IReadOnlyList<Enemy> Enemies { get; }
    public DialogueNpc Npc { get; }

    public Scene(
        Rectangle bounds,
        Vector2 playerStart,
        IReadOnlyList<Rectangle> platforms,
        IReadOnlyList<Rectangle> obstacles,
        IReadOnlyList<Rectangle> landmarks,
        IReadOnlyList<Enemy> enemies,
        DialogueNpc npc = null)
    {
        Bounds = bounds;
        PlayerStart = playerStart;
        Platforms = platforms;
        Obstacles = obstacles;
        Landmarks = landmarks;
        Enemies = enemies;
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

        foreach (var landmark in Landmarks)
        {
            spriteBatch.Draw(wallTexture, landmark, new Color(112, 101, 88));
            var ember = new Rectangle(landmark.Center.X - 2, landmark.Top + 14, 4, 12);
            spriteBatch.Draw(pixelTexture, ember, Color.Orange * 0.75f);
            spriteBatch.Draw(
                pixelTexture,
                new Rectangle(landmark.Left - 5, landmark.Bottom - 5, landmark.Width + 10, 5),
                new Color(8, 8, 10, 180));
        }

        foreach (var enemy in Enemies)
        {
            enemy.Draw(spriteBatch, enemyTexture, weaponTexture, pixelTexture);
        }
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
