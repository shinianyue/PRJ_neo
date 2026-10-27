using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace test.Combat;

public sealed class CombatEffects
{
    private sealed class Spark
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Rotation;
        public float Length;
        public float Thickness;
        public float Lifetime;
        public float MaxLifetime;
        public Color Color;
    }

    private readonly List<Spark> _sparks = new();
    public bool ReduceMotion { get; set; }

    public void AddParrySpark(Vector2 position)
    {
        AddBurst(position, Color.Gold, 14, 190f, 5f);
        AddBurst(position, Color.White, 6, 105f, 3f);
    }

    public void AddHitSpark(Vector2 position, Color color)
    {
        AddBurst(position, color, 8, 120f, 3.5f);
        AddBurst(position, Color.White, 3, 70f, 2f);
    }

    public void Update(GameTime gameTime)
    {
        var elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        for (var i = _sparks.Count - 1; i >= 0; i--)
        {
            var spark = _sparks[i];
            spark.Position += spark.Velocity * elapsed;
            spark.Velocity *= 1f - System.Math.Min(1f, elapsed * 2.5f);
            spark.Lifetime -= elapsed;
            if (spark.Lifetime <= 0f)
            {
                _sparks.RemoveAt(i);
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (var spark in _sparks)
        {
            var alpha = MathHelper.Clamp(spark.Lifetime / spark.MaxLifetime, 0f, 1f);
            spriteBatch.Draw(
                pixel,
                spark.Position,
                null,
                spark.Color * alpha,
                spark.Rotation,
                new Vector2(0.5f, 0.5f),
                new Vector2(spark.Length * alpha, spark.Thickness * alpha),
                SpriteEffects.None,
                0f);
        }
    }

    public void Clear()
    {
        _sparks.Clear();
    }

    private void AddBurst(Vector2 position, Color color, int count, float speed, float length)
    {
        if (ReduceMotion)
        {
            return;
        }

        for (var i = 0; i < count; i++)
        {
            var angle = MathHelper.TwoPi * i / count;
            var variation = 0.75f + (i % 3) * 0.18f;
            var velocity = new Vector2(
                (float)System.Math.Cos(angle) * speed * variation,
                (float)System.Math.Sin(angle) * speed * variation);
            var lifetime = 0.12f + (i % 4) * 0.025f;
            _sparks.Add(new Spark
            {
                Position = position,
                Velocity = velocity,
                Rotation = angle,
                Length = length * variation,
                Thickness = System.Math.Max(1f, length * 0.55f),
                Lifetime = lifetime,
                MaxLifetime = lifetime,
                Color = color
            });
        }
    }
}
