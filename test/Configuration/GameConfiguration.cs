using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using test.Combat;
using test.Combat.HitDetection;
using test.Entities;

namespace test.Configuration;

public sealed class GameConfiguration
{
    public WeaponConfiguration Weapon { get; set; } = new();
    public Dictionary<string, EnemyConfiguration> EnemyArchetypes { get; set; } = new();
    public List<SceneConfiguration> Scenes { get; set; } = new();

    public static GameConfiguration Load(string path)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var configuration = JsonSerializer.Deserialize<GameConfiguration>(File.ReadAllText(path), options)
            ?? throw new InvalidDataException($"Game configuration is empty: {path}");
        configuration.Validate(path);
        return configuration;
    }

    public WeaponDefinition CreateWeapon(Texture2D texture)
    {
        var attacks = Weapon.Attacks.Select(attack =>
        {
            if (!Enum.TryParse<Keys>(attack.InputKey, true, out var inputKey) ||
                !Enum.IsDefined(inputKey))
            {
                throw new InvalidDataException($"Unknown attack input key '{attack.InputKey}'.");
            }

            IHitRule hitRule = attack.HitRule.ToLowerInvariant() switch
            {
                "pixelperfect" => new PixelPerfectHitRule(),
                "bounds" => new BoundsHitRule(),
                _ => throw new InvalidDataException($"Unknown attack hit rule '{attack.HitRule}'.")
            };

            return new AttackDefinition(
                inputKey,
                attack.Damage,
                attack.Duration,
                attack.Cooldown,
                attack.ActiveStart,
                attack.ActiveEnd,
                attack.StartAngle,
                attack.EndAngle,
                attack.DashDistance,
                hitRule,
                attack.Name,
                attack.PostureDamage,
                ParseColor(attack.TrailColor));
        }).ToArray();

        return new WeaponDefinition(
            texture,
            new Vector2(Weapon.GripX, Weapon.GripY),
            Weapon.IdleAngle,
            attacks,
            Weapon.ComboResetDelay);
    }

    public Enemy CreateEnemy(string archetypeId, Rectangle bounds)
    {
        if (!EnemyArchetypes.TryGetValue(archetypeId, out var configuration))
        {
            throw new InvalidDataException($"Unknown enemy archetype '{archetypeId}'.");
        }

        if (!Enum.TryParse<EnemyStyle>(configuration.Style, true, out var style))
        {
            throw new InvalidDataException($"Unknown enemy style '{configuration.Style}'.");
        }

        var definition = new EnemyDefinition(
            style,
            configuration.MaxHealth,
            configuration.MaxPosture,
            configuration.PostureBreakDuration,
            configuration.MoveSpeed,
            configuration.WeaponIdleAngle,
            configuration.Width,
            configuration.Height,
            ParseColor(configuration.Tint),
            configuration.Attacks.Select(CreateEnemyAttack).ToArray(),
            configuration.Ai);
        return new Enemy(bounds, definition, EnemyAiFactory.Create(configuration.Ai));
    }

    private static EnemyAttackDefinition CreateEnemyAttack(EnemyAttackConfiguration attack)
    {
        if (!Enum.TryParse<EnemyHitboxShape>(attack.HitboxShape, true, out var hitboxShape) ||
            !Enum.IsDefined(hitboxShape))
        {
            throw new InvalidDataException($"Unknown enemy hitbox shape '{attack.HitboxShape}'.");
        }

        if (!Enum.TryParse<EnemyWeaponKind>(attack.WeaponKind, true, out var weaponKind) ||
            !Enum.IsDefined(weaponKind))
        {
            throw new InvalidDataException($"Unknown enemy weapon kind '{attack.WeaponKind}'.");
        }

        return new EnemyAttackDefinition(
            attack.Name,
            attack.TriggerRange,
            attack.Damage,
            attack.PostureDamage,
            attack.ParryPostureDamage,
            attack.WindupDuration,
            attack.CooldownDuration,
            weaponKind,
            new Vector2(attack.WeaponGripX, attack.WeaponGripY),
            new Vector2(attack.WeaponScaleX, attack.WeaponScaleY),
            attack.WeaponWindupAngle,
            attack.WeaponStrikeAngle,
            hitboxShape,
            new Vector2(attack.HitboxOffsetX, attack.HitboxOffsetY),
            attack.HitboxWidth,
            attack.HitboxHeight,
            attack.HitboxRadius);
    }

    private void Validate(string source)
    {
        if (Weapon == null ||
            Weapon.Attacks == null ||
            Weapon.Attacks.Count == 0 ||
            !float.IsFinite(Weapon.GripX) ||
            !float.IsFinite(Weapon.GripY) ||
            !float.IsFinite(Weapon.IdleAngle) ||
            !float.IsFinite(Weapon.ComboResetDelay) ||
            Weapon.ComboResetDelay <= 0f)
        {
            throw new InvalidDataException($"Weapon configuration is missing or invalid in {source}.");
        }

        if (EnemyArchetypes == null || EnemyArchetypes.Count == 0)
        {
            throw new InvalidDataException($"Enemy archetypes are missing in {source}.");
        }

        if (Scenes == null || Scenes.Count == 0)
        {
            throw new InvalidDataException($"Scene definitions are missing in {source}.");
        }

        foreach (var (id, enemy) in EnemyArchetypes)
        {
            if (string.IsNullOrWhiteSpace(id) ||
                enemy == null ||
                enemy.MaxHealth <= 0 ||
                !float.IsFinite(enemy.MaxPosture) ||
                enemy.MaxPosture <= 0f ||
                !float.IsFinite(enemy.PostureBreakDuration) ||
                enemy.PostureBreakDuration <= 0f ||
                !float.IsFinite(enemy.MoveSpeed) ||
                enemy.MoveSpeed <= 0f ||
                !float.IsFinite(enemy.WeaponIdleAngle) ||
                enemy.Width <= 0 ||
                enemy.Height <= 0 ||
                enemy.Attacks == null ||
                enemy.Attacks.Count == 0 ||
                !Enum.TryParse<EnemyStyle>(enemy.Style, true, out var enemyStyle) ||
                !Enum.IsDefined(enemyStyle))
            {
                throw new InvalidDataException($"Enemy archetype '{id}' contains invalid values in {source}.");
            }

            _ = ParseColor(enemy.Tint);
            _ = EnemyAiFactory.Create(enemy.Ai);
            foreach (var attack in enemy.Attacks)
            {
                ValidateEnemyAttack(attack, id, source);
            }
        }

        var sceneIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var scene in Scenes)
        {
            if (scene == null ||
                string.IsNullOrWhiteSpace(scene.Id) ||
                string.IsNullOrWhiteSpace(scene.Name) ||
                scene.Platforms == null ||
                scene.Obstacles == null ||
                scene.Landmarks == null ||
                scene.Enemies == null ||
                !sceneIds.Add(scene.Id) ||
                !float.IsFinite(scene.WidthInViewports) ||
                scene.WidthInViewports <= 0f ||
                !float.IsFinite(scene.PlayerStartXRatio) ||
                scene.PlayerStartXRatio < 0f ||
                scene.PlayerStartXRatio >= scene.WidthInViewports)
            {
                throw new InvalidDataException($"A scene definition contains invalid values in {source}.");
            }

            if (scene.Platforms.Any(platform =>
                    platform == null ||
                    !float.IsFinite(platform.XRatio) ||
                    !float.IsFinite(platform.YRatio) ||
                    !float.IsFinite(platform.WidthRatio) ||
                    platform.XRatio < 0f ||
                    platform.YRatio < 0f ||
                    platform.YRatio >= 1f ||
                    platform.WidthRatio <= 0f ||
                    platform.XRatio + platform.WidthRatio > scene.WidthInViewports) ||
                scene.Obstacles.Any(obstacle =>
                    obstacle == null ||
                    !float.IsFinite(obstacle.XRatio) ||
                    obstacle.XRatio < 0f ||
                    obstacle.XRatio >= scene.WidthInViewports) ||
                scene.Landmarks.Any(landmark =>
                    landmark == null ||
                    !float.IsFinite(landmark.XRatio) ||
                    landmark.XRatio < 0f ||
                    landmark.XRatio >= scene.WidthInViewports ||
                    landmark.Width <= 0 ||
                    landmark.Height <= 0))
            {
                throw new InvalidDataException($"Scene '{scene.Id}' contains invalid geometry in {source}.");
            }

            foreach (var enemy in scene.Enemies)
            {
                if (enemy == null ||
                    string.IsNullOrWhiteSpace(enemy.Archetype) ||
                    !EnemyArchetypes.ContainsKey(enemy.Archetype) ||
                    !float.IsFinite(enemy.XRatio) ||
                    enemy.XRatio < 0f ||
                    enemy.XRatio >= scene.WidthInViewports)
                {
                    throw new InvalidDataException($"Scene '{scene.Id}' contains an invalid enemy spawn in {source}.");
                }
            }

            if (scene.Npc != null &&
                (string.IsNullOrWhiteSpace(scene.Npc.Name) ||
                 string.IsNullOrWhiteSpace(scene.Npc.SequenceId) ||
                 !float.IsFinite(scene.Npc.XRatio) ||
                 scene.Npc.XRatio < 0f ||
                 scene.Npc.XRatio >= scene.WidthInViewports))
            {
                throw new InvalidDataException($"Scene '{scene.Id}' contains an invalid NPC definition in {source}.");
            }
        }

        foreach (var attack in Weapon.Attacks)
        {
            if (attack == null)
            {
                throw new InvalidDataException($"An attack definition is missing in {source}.");
            }

            if (string.IsNullOrWhiteSpace(attack.InputKey) ||
                string.IsNullOrWhiteSpace(attack.HitRule) ||
                string.IsNullOrWhiteSpace(attack.Name))
            {
                throw new InvalidDataException($"An attack definition is incomplete in {source}.");
            }

            if (!Enum.TryParse<Keys>(attack.InputKey, true, out var attackKey) ||
                !Enum.IsDefined(attackKey) ||
                (attack.HitRule.ToLowerInvariant() != "pixelperfect" &&
                 attack.HitRule.ToLowerInvariant() != "bounds"))
            {
                throw new InvalidDataException($"Attack '{attack.Name}' uses an unknown input key or hit rule in {source}.");
            }

            _ = ParseColor(attack.TrailColor);
            _ = new AttackDefinition(
                attackKey,
                attack.Damage,
                attack.Duration,
                attack.Cooldown,
                attack.ActiveStart,
                attack.ActiveEnd,
                attack.StartAngle,
                attack.EndAngle,
                attack.DashDistance,
                attack.HitRule.Equals("bounds", StringComparison.OrdinalIgnoreCase)
                    ? new BoundsHitRule()
                    : new PixelPerfectHitRule(),
                attack.Name,
                attack.PostureDamage,
                ParseColor(attack.TrailColor));
        }
    }

    internal static Color ParseColor(string value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            (value.Length != 7 && value.Length != 9) ||
            value[0] != '#')
        {
            throw new InvalidDataException($"Color '{value}' must use #RRGGBB or #RRGGBBAA format.");
        }

        try
        {
            var red = byte.Parse(value.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var green = byte.Parse(value.Substring(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var blue = byte.Parse(value.Substring(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var alpha = value.Length == 9
                ? byte.Parse(value.Substring(7, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                : byte.MaxValue;
            return new Color(red, green, blue, alpha);
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException($"Color '{value}' is not valid hexadecimal.", exception);
        }
    }

    private static void ValidateEnemyAttack(EnemyAttackConfiguration attack, string archetype, string source)
    {
        if (attack == null ||
                string.IsNullOrWhiteSpace(attack.Name) ||
                !float.IsFinite(attack.TriggerRange) ||
                attack.TriggerRange <= 0f ||
                attack.Damage <= 0 ||
                !float.IsFinite(attack.PostureDamage) ||
                attack.PostureDamage < 0f ||
                !float.IsFinite(attack.ParryPostureDamage) ||
                attack.ParryPostureDamage < 0f ||
                !float.IsFinite(attack.WindupDuration) ||
                attack.WindupDuration <= 0f ||
                !float.IsFinite(attack.CooldownDuration) ||
                attack.CooldownDuration <= 0f ||
                !float.IsFinite(attack.WeaponGripX) ||
                !float.IsFinite(attack.WeaponGripY) ||
                !float.IsFinite(attack.WeaponScaleX) ||
                attack.WeaponScaleX <= 0f ||
                !float.IsFinite(attack.WeaponScaleY) ||
                attack.WeaponScaleY <= 0f ||
                !float.IsFinite(attack.WeaponWindupAngle) ||
                !float.IsFinite(attack.WeaponStrikeAngle) ||
                !float.IsFinite(attack.HitboxOffsetX) ||
                !float.IsFinite(attack.HitboxOffsetY) ||
                !float.IsFinite(attack.HitboxRadius) ||
                attack.HitboxWidth < 0 ||
                attack.HitboxHeight < 0 ||
                !Enum.TryParse<EnemyHitboxShape>(attack.HitboxShape, true, out var shape) ||
                !Enum.IsDefined(shape) ||
                !Enum.TryParse<EnemyWeaponKind>(attack.WeaponKind, true, out var weaponKind) ||
                !Enum.IsDefined(weaponKind) ||
                (shape == EnemyHitboxShape.Rectangle && (attack.HitboxWidth <= 0 || attack.HitboxHeight <= 0)) ||
                (shape == EnemyHitboxShape.Circle && attack.HitboxRadius <= 0f))
        {
            throw new InvalidDataException($"Enemy archetype '{archetype}' has an invalid attack definition in {source}.");
        }
    }
}

public sealed class WeaponConfiguration
{
    public float GripX { get; set; }
    public float GripY { get; set; }
    public float IdleAngle { get; set; }
    public float ComboResetDelay { get; set; }
    public List<AttackConfiguration> Attacks { get; set; } = new();
}

public sealed class AttackConfiguration
{
    public string InputKey { get; set; } = string.Empty;
    public int Damage { get; set; }
    public float Duration { get; set; }
    public float Cooldown { get; set; }
    public float ActiveStart { get; set; }
    public float ActiveEnd { get; set; }
    public float StartAngle { get; set; }
    public float EndAngle { get; set; }
    public float DashDistance { get; set; }
    public string HitRule { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public float PostureDamage { get; set; }
    public string TrailColor { get; set; } = "#FFD700";
}

public sealed class EnemyConfiguration
{
    public string Style { get; set; } = "Swordsman";
    public int MaxHealth { get; set; }
    public float MaxPosture { get; set; } = 100f;
    public float PostureBreakDuration { get; set; } = 3.5f;
    public float MoveSpeed { get; set; }
    public float WeaponIdleAngle { get; set; } = -0.35f;
    public int Width { get; set; } = 40;
    public int Height { get; set; } = 58;
    public string Tint { get; set; } = "#FFFFFF";
    public string Ai { get; set; } = "default";
    public List<EnemyAttackConfiguration> Attacks { get; set; } = new();
}

public sealed class EnemyAttackConfiguration
{
    public string Name { get; set; } = string.Empty;
    public float TriggerRange { get; set; }
    public int Damage { get; set; }
    public float PostureDamage { get; set; }
    public float ParryPostureDamage { get; set; }
    public float WindupDuration { get; set; }
    public float CooldownDuration { get; set; }
    public string WeaponKind { get; set; } = "Sword";
    public float WeaponGripX { get; set; } = 8f;
    public float WeaponGripY { get; set; } = 48f;
    public float WeaponScaleX { get; set; } = 1f;
    public float WeaponScaleY { get; set; } = 1f;
    public float WeaponWindupAngle { get; set; } = -1.1f;
    public float WeaponStrikeAngle { get; set; } = 0.3f;
    public string HitboxShape { get; set; } = "Rectangle";
    public float HitboxOffsetX { get; set; }
    public float HitboxOffsetY { get; set; }
    public int HitboxWidth { get; set; } = 60;
    public int HitboxHeight { get; set; } = 36;
    public float HitboxRadius { get; set; } = 30f;
}

public sealed class SceneConfiguration
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TransitionSubtitle { get; set; } = string.Empty;
    public float WidthInViewports { get; set; } = 2f;
    public float PlayerStartXRatio { get; set; } = 0.05f;
    public List<PlatformConfiguration> Platforms { get; set; } = new();
    public List<ObstacleConfiguration> Obstacles { get; set; } = new();
    public List<LandmarkConfiguration> Landmarks { get; set; } = new();
    public List<EnemySpawnConfiguration> Enemies { get; set; } = new();
    public NpcConfiguration Npc { get; set; }
}

public sealed class PlatformConfiguration
{
    public float XRatio { get; set; }
    public float YRatio { get; set; }
    public float WidthRatio { get; set; }
}

public sealed class ObstacleConfiguration
{
    public float XRatio { get; set; }
}

public sealed class LandmarkConfiguration
{
    public float XRatio { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public sealed class EnemySpawnConfiguration
{
    public string Archetype { get; set; } = string.Empty;
    public float XRatio { get; set; }
}

public sealed class NpcConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string SequenceId { get; set; } = string.Empty;
    public float XRatio { get; set; }
    public bool Active { get; set; } = true;
}
