using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;
using test.Combat.HitDetection;
using test.Entities;

namespace test.Combat;

public sealed class Weapon
{
    private readonly WeaponDefinition _definition;
    private readonly Color[] _pixels;
    private readonly Keys[] _comboKeys;
    private readonly Dictionary<Keys, int> _comboIndices = new();
    private readonly Dictionary<Keys, float> _comboTimers = new();
    private readonly HashSet<Enemy> _hitEnemies = new();
    private KeyboardState _previousKeyboardState;
    private float _attackTimer;
    private float _cooldownTimer;
    private AttackDefinition _activeAttack;

    public bool IsAttacking => _attackTimer > 0f;
    public string ActiveAttackName => IsAttacking ? _activeAttack?.Name : null;

    public Weapon(WeaponDefinition definition)
    {
        _definition = definition ?? throw new System.ArgumentNullException(nameof(definition));
        _pixels = new Color[_definition.Texture.Width * _definition.Texture.Height];
        _definition.Texture.GetData(_pixels);
        _comboKeys = _definition.Attacks.Select(attack => attack.InputKey).Distinct().ToArray();
        foreach (var key in _comboKeys)
        {
            _comboIndices.Add(key, 0);
            _comboTimers.Add(key, 0f);
        }
    }

    public void Reset()
    {
        _attackTimer = 0f;
        _cooldownTimer = 0f;
        foreach (var key in _comboKeys)
        {
            _comboIndices[key] = 0;
            _comboTimers[key] = 0f;
        }
        _activeAttack = null;
        _hitEnemies.Clear();
        _previousKeyboardState = Keyboard.GetState();
    }

    public void Update(GameTime gameTime, Player player)
    {
        var keyboardState = Keyboard.GetState();
        var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _cooldownTimer = System.Math.Max(0f, _cooldownTimer - elapsedSeconds);
        foreach (var key in _comboKeys)
        {
            _comboTimers[key] = System.Math.Max(0f, _comboTimers[key] - elapsedSeconds);
            if (!keyboardState.IsKeyDown(key) ||
                _previousKeyboardState.IsKeyDown(key) ||
                _cooldownTimer > 0f)
            {
                continue;
            }

            var comboCount = _definition.Attacks.Count(attack => attack.InputKey == key);
            var comboIndex = _comboTimers[key] <= 0f ? 0 : _comboIndices[key];
            var comboAttackIndex = 0;
            foreach (var attack in _definition.Attacks)
            {
                if (attack.InputKey != key)
                {
                    continue;
                }

                if (comboAttackIndex++ == comboIndex)
                {
                    _activeAttack = attack;
                    break;
                }
            }

            _attackTimer = _activeAttack.Duration;
            _cooldownTimer = _activeAttack.Cooldown;
            _comboTimers[key] = _definition.ComboResetDelay;
            _hitEnemies.Clear();
            _comboIndices[key] = (comboIndex + 1) % comboCount;

            if (_activeAttack.DashDistance > 0f)
            {
                player.StartDash(_activeAttack.DashDistance, _activeAttack.Duration);
            }
            break;
        }

        if (_attackTimer > 0f)
        {
            _attackTimer = System.Math.Max(0f, _attackTimer - elapsedSeconds);
        }

        _previousKeyboardState = keyboardState;
    }

    public bool TryHit(Enemy enemy, Player player)
    {
        var attack = _activeAttack;
        if (attack == null)
        {
            return false;
        }

        var activeTime = attack.Duration - _attackTimer;
        if (!IsAttacking ||
            _hitEnemies.Contains(enemy) ||
            activeTime < attack.ActiveStart ||
            activeTime > attack.ActiveEnd)
        {
            return false;
        }

        if (!enemy.IsAlive || !IntersectsTarget(enemy.Bounds, player))
        {
            return false;
        }

        enemy.TakeDamage(attack.Damage);
        enemy.AddPostureDamage(attack.PostureDamage);
        _hitEnemies.Add(enemy);
        return true;
    }

    private bool IntersectsTarget(Rectangle target, Player player)
    {
        var (position, angle, effects, origin) = GetDrawPose(player);
        return _activeAttack.HitRule.Intersects(
            _definition.Texture,
            _pixels,
            target,
            position,
            angle,
            effects,
            origin);
    }

    public void Draw(SpriteBatch spriteBatch, Player player, Texture2D pixelTexture)
    {
        var (position, angle, effects, origin) = GetDrawPose(player);
        if (!IsAttacking)
        {
            spriteBatch.Draw(_definition.Texture, position, null, Color.White, angle, origin, 1f, effects, 0f);
            return;
        }

        var progress = 1f - _attackTimer / _activeAttack.Duration;
        var trailActive = progress >= _activeAttack.ActiveStart / _activeAttack.Duration &&
            progress <= _activeAttack.ActiveEnd / _activeAttack.Duration;
        if (trailActive)
        {
            var facing = player.FacingRight ? 1f : -1f;
            var trailAngle = angle + (player.FacingRight ? -0.65f : 0.65f);
            var trailStart = position + new Vector2(facing * 8f, -10f);
            var trailLength = 34f + _activeAttack.Damage * 0.18f;
            var trailThickness = 2f + MathHelper.Clamp(_activeAttack.Damage / 20f, 0f, 3f);
            var fade = 0.8f * (1f - MathHelper.Clamp(
                (progress - _activeAttack.ActiveStart / _activeAttack.Duration) /
                System.Math.Max(0.01f, (_activeAttack.ActiveEnd - _activeAttack.ActiveStart) / _activeAttack.Duration),
                0f,
                1f) * 0.55f);
            var trailColor = _activeAttack.TrailColor * fade;
            for (var i = 0; i < 3; i++)
            {
                var arcAngle = trailAngle + (i - 1) * 0.18f;
                spriteBatch.Draw(
                    pixelTexture,
                    trailStart,
                    null,
                    trailColor * (1f - i * 0.2f),
                    arcAngle,
                    new Vector2(0f, 0.5f),
                    new Vector2(trailLength - i * 5f, trailThickness - i * 0.35f),
                    SpriteEffects.None,
                    0f);
            }
        }

        spriteBatch.Draw(_definition.Texture, position, null, Color.White, angle, origin, 1f, effects, 0f);
    }

    private (Vector2 Position, float Angle, SpriteEffects Effects, Vector2 Origin) GetDrawPose(Player player)
    {
        var attack = _activeAttack;
        var angle = IsAttacking
            ? MathHelper.Lerp(
                attack.StartAngle,
                attack.EndAngle,
                1f - (_attackTimer / attack.Duration))
            : _definition.IdleAngle;
        if (!player.FacingRight)
        {
            angle = -angle;
        }

        var position = player.HandPosition;
        var effects = player.FacingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        var grip = _definition.GripOrigin;
        var origin = player.FacingRight
            ? grip
            : new Vector2(_definition.Texture.Width - grip.X, grip.Y);
        return (position, angle, effects, origin);
    }
}
