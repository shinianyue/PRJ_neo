using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace test.Combat;

public sealed class WeaponDefinition
{
    public Texture2D Texture { get; }
    public Vector2 GripOrigin { get; }
    public float IdleAngle { get; }
    public IReadOnlyList<AttackDefinition> Attacks { get; }
    public float ComboResetDelay { get; }

    public WeaponDefinition(
        Texture2D texture,
        Vector2 gripOrigin,
        float idleAngle,
        IReadOnlyList<AttackDefinition> attacks,
        float comboResetDelay)
    {
        Texture = texture ?? throw new System.ArgumentNullException(nameof(texture));
        GripOrigin = gripOrigin;
        IdleAngle = idleAngle;
        Attacks = attacks ?? throw new System.ArgumentNullException(nameof(attacks));
        if (attacks.Count == 0)
        {
            throw new System.ArgumentException("A weapon must define at least one attack.", nameof(attacks));
        }

        if (comboResetDelay <= 0f)
        {
            throw new System.ArgumentOutOfRangeException(nameof(comboResetDelay));
        }

        ComboResetDelay = comboResetDelay;
    }
}
