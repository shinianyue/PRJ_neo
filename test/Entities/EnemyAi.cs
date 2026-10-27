using Microsoft.Xna.Framework;

namespace test.Entities;

public interface IEnemyAi
{
    string Name { get; }
    void Reset(Enemy enemy);
    void Update(EnemyAiContext context);
}

public sealed class EnemyAiContext
{
    public Enemy Enemy { get; }
    public Player Player { get; }
    public Rectangle SceneBounds { get; }
    public float ElapsedSeconds { get; }

    public EnemyAiContext(Enemy enemy, Player player, Rectangle sceneBounds, float elapsedSeconds)
    {
        Enemy = enemy;
        Player = player;
        SceneBounds = sceneBounds;
        ElapsedSeconds = elapsedSeconds;
    }
}

public sealed class DefaultEnemyAi : IEnemyAi
{
    public string Name => "default";

    public void Reset(Enemy enemy)
    {
        enemy.AttackWindupTimer = 0f;
        enemy.AttackCooldownTimer = 0f;
        enemy.PostureBreakTimer = 0f;
        enemy.HitFlashTimer = 0f;
        enemy.Posture = 0f;
        enemy.AttackImpactPending = false;
    }

    public void Update(EnemyAiContext context)
    {
        var enemy = context.Enemy;
        var player = context.Player;
        var sceneBounds = context.SceneBounds;
        var elapsedSeconds = context.ElapsedSeconds;

        enemy.AttackImpactPending = false;
        if (!enemy.IsAlive)
        {
            return;
        }

        enemy.HitFlashTimer = System.Math.Max(0f, enemy.HitFlashTimer - elapsedSeconds);
        if (enemy.IsPostureBroken)
        {
            enemy.PostureBreakTimer = System.Math.Max(0f, enemy.PostureBreakTimer - elapsedSeconds);
            if (enemy.PostureBreakTimer == 0f)
            {
                enemy.Posture = enemy.MaxPosture * 0.65f;
            }

            return;
        }

        enemy.AttackCooldownTimer = System.Math.Max(0f, enemy.AttackCooldownTimer - elapsedSeconds);
        var horizontalDistance = player.Bounds.Center.X - enemy.Bounds.Center.X;
        var verticallyAligned = enemy.Bounds.Bottom > player.Bounds.Top && enemy.Bounds.Top < player.Bounds.Bottom;
        enemy.FacingRight = horizontalDistance > 0;

        if (enemy.AttackWindupTimer > 0f)
        {
            enemy.AttackWindupTimer -= elapsedSeconds;
            if (enemy.AttackWindupTimer <= 0f)
            {
                enemy.AttackImpactPending = true;
            }

            return;
        }

        if (enemy.AttackCooldownTimer <= 0f &&
            verticallyAligned &&
            enemy.TryBeginAttack(horizontalDistance, 1f, 1f, 1f))
        {
            return;
        }

        if (System.Math.Abs(horizontalDistance) > enemy.EngageRange)
        {
            var direction = System.Math.Sign(horizontalDistance);
            var nextX = enemy.Bounds.X + direction * enemy.MoveSpeed * elapsedSeconds;
            nextX = MathHelper.Clamp(nextX, sceneBounds.Left, sceneBounds.Right - enemy.Bounds.Width);
            enemy.Bounds = new Rectangle((int)nextX, enemy.Bounds.Y, enemy.Bounds.Width, enemy.Bounds.Height);
        }
    }
}

public sealed class AggressiveEnemyAi : IEnemyAi
{
    public string Name => "aggressive";

    public void Reset(Enemy enemy)
    {
        enemy.AttackCooldownTimer = 0.15f;
    }

    public void Update(EnemyAiContext context)
    {
        var enemy = context.Enemy;
        var player = context.Player;
        var sceneBounds = context.SceneBounds;
        var elapsedSeconds = context.ElapsedSeconds;

        enemy.AttackImpactPending = false;
        if (!enemy.IsAlive)
        {
            return;
        }

        enemy.HitFlashTimer = System.Math.Max(0f, enemy.HitFlashTimer - elapsedSeconds);
        if (enemy.IsPostureBroken)
        {
            enemy.PostureBreakTimer = System.Math.Max(0f, enemy.PostureBreakTimer - elapsedSeconds);
            if (enemy.PostureBreakTimer == 0f)
            {
                enemy.Posture = enemy.MaxPosture * 0.65f;
            }

            return;
        }

        var horizontalDistance = player.Bounds.Center.X - enemy.Bounds.Center.X;
        var verticallyAligned = enemy.Bounds.Bottom > player.Bounds.Top && enemy.Bounds.Top < player.Bounds.Bottom;
        enemy.FacingRight = horizontalDistance > 0;
        enemy.AttackCooldownTimer = System.Math.Max(0f, enemy.AttackCooldownTimer - elapsedSeconds);

        if (enemy.AttackWindupTimer > 0f)
        {
            enemy.AttackWindupTimer -= elapsedSeconds;
            if (enemy.AttackWindupTimer <= 0f)
            {
                enemy.AttackImpactPending = true;
            }

            return;
        }

        if (enemy.AttackCooldownTimer <= 0f &&
            verticallyAligned &&
            enemy.TryBeginAttack(horizontalDistance, 1.15f, 0.75f, 0.7f))
        {
            return;
        }

        var closeEnoughToRush = System.Math.Abs(horizontalDistance) > enemy.EngageRange * 1.4f;
        if (closeEnoughToRush)
        {
            var direction = System.Math.Sign(horizontalDistance);
            var step = enemy.MoveSpeed * 1.4f * elapsedSeconds;
            var nextX = enemy.Bounds.X + direction * step;
            nextX = MathHelper.Clamp(nextX, sceneBounds.Left, sceneBounds.Right - enemy.Bounds.Width);
            enemy.Bounds = new Rectangle((int)nextX, enemy.Bounds.Y, enemy.Bounds.Width, enemy.Bounds.Height);
        }
    }

}

public static class EnemyAiFactory
{
    public static IEnemyAi Create(string name)
    {
        return name?.ToLowerInvariant() switch
        {
            "default" => new DefaultEnemyAi(),
            "aggressive" => new AggressiveEnemyAi(),
            _ => throw new System.IO.InvalidDataException($"Unknown enemy AI '{name}'.")
        };
    }
}
