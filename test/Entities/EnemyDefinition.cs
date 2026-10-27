using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace test.Entities;

public sealed class EnemyDefinition
{
    public EnemyStyle Style { get; }
    public int MaxHealth { get; }
    public float MaxPosture { get; }
    public float PostureBreakDuration { get; }
    public float MoveSpeed { get; }
    public float WeaponIdleAngle { get; }
    public int Width { get; }
    public int Height { get; }
    public Color Tint { get; }
    public IReadOnlyList<EnemyAttackDefinition> Attacks { get; }
    public string AiName { get; }

    public EnemyDefinition(
        EnemyStyle style,
        int maxHealth,
        float maxPosture,
        float postureBreakDuration,
        float moveSpeed,
        float weaponIdleAngle,
        int width,
        int height,
        Color tint,
        IReadOnlyList<EnemyAttackDefinition> attacks,
        string aiName)
    {
        Style = style;
        MaxHealth = maxHealth;
        MaxPosture = maxPosture;
        PostureBreakDuration = postureBreakDuration;
        MoveSpeed = moveSpeed;
        WeaponIdleAngle = weaponIdleAngle;
        Width = width;
        Height = height;
        Tint = tint;
        Attacks = attacks;
        AiName = aiName;
    }
}
