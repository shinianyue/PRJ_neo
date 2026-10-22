using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using test.Combat.HitDetection;
using test.Entities;

namespace test.Combat;

public sealed class Weapon
{
    private readonly WeaponDefinition _definition;
    private readonly Color[] _pixels;
    private KeyboardState _previousKeyboardState;
    private float _attackTimer;
    private float _cooldownTimer;
    private float _comboTimer;
    private int _comboIndex;
    private AttackDefinition _activeAttack;
    private bool _hasHit;

    public bool IsAttacking => _attackTimer > 0f;

    public Weapon(WeaponDefinition definition)
    {
        _definition = definition ?? throw new System.ArgumentNullException(nameof(definition));
        _pixels = new Color[_definition.Texture.Width * _definition.Texture.Height];
        _definition.Texture.GetData(_pixels);
    }

    public void Reset()
    {
        _attackTimer = 0f;
        _cooldownTimer = 0f;
        _comboTimer = 0f;
        _comboIndex = 0;
        _activeAttack = null;
        _hasHit = false;
        _previousKeyboardState = Keyboard.GetState();
    }

    public void Update(GameTime gameTime, Player player)
    {
        var keyboardState = Keyboard.GetState();
        var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _cooldownTimer = System.Math.Max(0f, _cooldownTimer - elapsedSeconds);
        _comboTimer = System.Math.Max(0f, _comboTimer - elapsedSeconds);

        var inputKey = _definition.Attacks[0].InputKey;
        if (keyboardState.IsKeyDown(inputKey) &&
            !_previousKeyboardState.IsKeyDown(inputKey) &&
            _cooldownTimer <= 0f)
        {
            if (_comboTimer <= 0f)
            {
                _comboIndex = 0;
            }

            _activeAttack = _definition.Attacks[_comboIndex];
            _attackTimer = _activeAttack.Duration;
            _cooldownTimer = _activeAttack.Cooldown;
            _comboTimer = _definition.ComboResetDelay;
            _hasHit = false;
            _comboIndex = (_comboIndex + 1) % _definition.Attacks.Count;

            if (_activeAttack.DashDistance > 0f)
            {
                player.StartDash(_activeAttack.DashDistance, _activeAttack.Duration);
            }
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
        if (!IsAttacking || _hasHit || activeTime < attack.ActiveStart || activeTime > attack.ActiveEnd)
        {
            return false;
        }

        if (!enemy.IsAlive || !IntersectsTarget(enemy.Bounds, player))
        {
            return false;
        }

        enemy.TakeDamage(attack.Damage);
        _hasHit = true;
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

    public void Draw(SpriteBatch spriteBatch, Player player)
    {
        var (position, angle, effects, origin) = GetDrawPose(player);
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
