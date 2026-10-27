using Microsoft.Xna.Framework;

namespace test.Entities;

public enum EnemyHitboxShape
{
    Rectangle,
    Circle
}

public enum EnemyWeaponKind
{
    Sword,
    Spear
}

public sealed class EnemyAttackDefinition
{
    public string Name { get; }
    public float TriggerRange { get; }
    public int Damage { get; }
    public float PostureDamage { get; }
    public float WindupDuration { get; }
    public float CooldownDuration { get; }
    public float ParryPostureDamage { get; }
    public EnemyWeaponKind WeaponKind { get; }
    public Vector2 WeaponGripOrigin { get; }
    public Vector2 WeaponScale { get; }
    public float WeaponWindupAngle { get; }
    public float WeaponStrikeAngle { get; }
    public EnemyHitboxShape HitboxShape { get; }
    public Vector2 HitboxOffset { get; }
    public int HitboxWidth { get; }
    public int HitboxHeight { get; }
    public float HitboxRadius { get; }

    public EnemyAttackDefinition(
        string name,
        float triggerRange,
        int damage,
        float postureDamage,
        float parryPostureDamage,
        float windupDuration,
        float cooldownDuration,
        EnemyWeaponKind weaponKind,
        Vector2 weaponGripOrigin,
        Vector2 weaponScale,
        float weaponWindupAngle,
        float weaponStrikeAngle,
        EnemyHitboxShape hitboxShape,
        Vector2 hitboxOffset,
        int hitboxWidth,
        int hitboxHeight,
        float hitboxRadius)
    {
        Name = name;
        TriggerRange = triggerRange;
        Damage = damage;
        PostureDamage = postureDamage;
        ParryPostureDamage = parryPostureDamage;
        WindupDuration = windupDuration;
        CooldownDuration = cooldownDuration;
        WeaponKind = weaponKind;
        WeaponGripOrigin = weaponGripOrigin;
        WeaponScale = weaponScale;
        WeaponWindupAngle = weaponWindupAngle;
        WeaponStrikeAngle = weaponStrikeAngle;
        HitboxShape = hitboxShape;
        HitboxOffset = hitboxOffset;
        HitboxWidth = hitboxWidth;
        HitboxHeight = hitboxHeight;
        HitboxRadius = hitboxRadius;
    }

    public Rectangle GetHitboxBounds(Rectangle enemyBounds, bool facingRight)
    {
        var centerX = enemyBounds.Center.X +
            (int)System.MathF.Round(facingRight ? HitboxOffset.X : -HitboxOffset.X);
        var centerY = enemyBounds.Top + (int)System.MathF.Round(HitboxOffset.Y);
        if (HitboxShape == EnemyHitboxShape.Circle)
        {
            var diameter = (int)System.Math.Ceiling(HitboxRadius * 2f);
            return new Rectangle(centerX - diameter / 2, centerY - diameter / 2, diameter, diameter);
        }

        return new Rectangle(
            centerX - HitboxWidth / 2,
            centerY - HitboxHeight / 2,
            HitboxWidth,
            HitboxHeight);
    }

    public bool Intersects(Rectangle enemyBounds, bool facingRight, Rectangle target)
    {
        var hitbox = GetHitboxBounds(enemyBounds, facingRight);
        if (HitboxShape == EnemyHitboxShape.Rectangle)
        {
            return hitbox.Intersects(target);
        }

        var closestX = MathHelper.Clamp(hitbox.Center.X, target.Left, target.Right);
        var closestY = MathHelper.Clamp(hitbox.Center.Y, target.Top, target.Bottom);
        var deltaX = hitbox.Center.X - closestX;
        var deltaY = hitbox.Center.Y - closestY;
        return deltaX * deltaX + deltaY * deltaY <= HitboxRadius * HitboxRadius;
    }
}
