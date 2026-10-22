using Microsoft.Xna.Framework.Input;
using test.Combat.HitDetection;

namespace test.Combat;

public sealed class AttackDefinition
{
    public Keys InputKey { get; }
    public int Damage { get; }
    public float Duration { get; }
    public float Cooldown { get; }
    public float ActiveStart { get; }
    public float ActiveEnd { get; }
    public float StartAngle { get; }
    public float EndAngle { get; }
    public float DashDistance { get; }
    public IHitRule HitRule { get; }

    public AttackDefinition(
        Keys inputKey,
        int damage,
        float duration,
        float cooldown,
        float activeStart,
        float activeEnd,
        float startAngle,
        float endAngle,
        float dashDistance,
        IHitRule hitRule)
    {
        if (damage <= 0 ||
            duration <= 0f ||
            cooldown < 0f ||
            activeStart < 0f ||
            activeEnd < activeStart ||
            activeEnd > duration ||
            dashDistance < 0f)
        {
            throw new System.ArgumentOutOfRangeException(nameof(duration), "Attack timing and damage values are invalid.");
        }

        InputKey = inputKey;
        Damage = damage;
        Duration = duration;
        Cooldown = cooldown;
        ActiveStart = activeStart;
        ActiveEnd = activeEnd;
        StartAngle = startAngle;
        EndAngle = endAngle;
        DashDistance = dashDistance;
        HitRule = hitRule ?? throw new System.ArgumentNullException(nameof(hitRule));
    }
}
