using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using test.Entities;

namespace test.Combat;

public sealed class Weapon
{
    private const float AttackDuration = 0.32f;
    private const float CooldownDuration = 0.45f;
    private const float ActiveStart = 0.08f;
    private const float ActiveEnd = 0.25f;
    private readonly Texture2D _texture;
    private readonly Color[] _pixels;
    private KeyboardState _previousKeyboardState;
    private float _attackTimer;
    private float _cooldownTimer;
    private bool _hasHit;

    public bool IsAttacking => _attackTimer > 0f;

    public Weapon(Texture2D texture)
    {
        _texture = texture;
        _pixels = new Color[texture.Width * texture.Height];
        texture.GetData(_pixels);
    }

    public void Reset()
    {
        _attackTimer = 0f;
        _cooldownTimer = 0f;
        _hasHit = false;
        _previousKeyboardState = Keyboard.GetState();
    }

    public void Update(GameTime gameTime, Player player)
    {
        var keyboardState = Keyboard.GetState();
        var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _cooldownTimer = System.Math.Max(0f, _cooldownTimer - elapsedSeconds);

        if (keyboardState.IsKeyDown(Keys.J) &&
            !_previousKeyboardState.IsKeyDown(Keys.J) &&
            _cooldownTimer <= 0f)
        {
            _attackTimer = AttackDuration;
            _cooldownTimer = CooldownDuration;
            _hasHit = false;
        }

        if (_attackTimer > 0f)
        {
            _attackTimer = System.Math.Max(0f, _attackTimer - elapsedSeconds);
        }

        _previousKeyboardState = keyboardState;
    }

    public bool TryHit(Enemy enemy, Player player)
    {
        var activeTime = AttackDuration - _attackTimer;
        if (!IsAttacking || _hasHit || activeTime < ActiveStart || activeTime > ActiveEnd)
        {
            return false;
        }

        if (!enemy.IsAlive || !IntersectsColoredPixels(enemy.Bounds, player))
        {
            return false;
        }

        enemy.TakeDamage(50);
        _hasHit = true;
        return true;
    }

    private bool IntersectsColoredPixels(Rectangle target, Player player)
    {
        var (position, angle, effects, origin) = GetDrawPose(player);
        var cos = (float)System.Math.Cos(angle);
        var sin = (float)System.Math.Sin(angle);

        for (var y = 0; y < _texture.Height; y++)
        {
            for (var x = 0; x < _texture.Width; x++)
            {
                if (_pixels[y * _texture.Width + x].A == 0)
                {
                    continue;
                }

                var localX = x + 0.5f - origin.X;
                var localY = y + 0.5f - origin.Y;
                if ((effects & SpriteEffects.FlipHorizontally) != 0)
                {
                    localX = _texture.Width - x - 0.5f - origin.X;
                }

                var worldX = position.X + localX * cos - localY * sin;
                var worldY = position.Y + localX * sin + localY * cos;
                if (target.Contains((int)System.Math.Floor(worldX), (int)System.Math.Floor(worldY)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void Draw(SpriteBatch spriteBatch, Player player)
    {
        var (position, angle, effects, origin) = GetDrawPose(player);
        spriteBatch.Draw(_texture, position, null, Color.White, angle, origin, 1f, effects, 0f);
    }

    private (Vector2 Position, float Angle, SpriteEffects Effects, Vector2 Origin) GetDrawPose(Player player)
    {
        var angle = IsAttacking
            ? MathHelper.Lerp(-1.2f, 0.8f, 1f - (_attackTimer / AttackDuration))
            : -0.35f;
        if (!player.FacingRight)
        {
            angle = -angle;
        }

        var position = player.HandPosition;
        var effects = player.FacingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        var grip = GetGripOrigin();
        var origin = player.FacingRight
            ? grip
            : new Vector2(_texture.Width - grip.X, grip.Y);
        return (position, angle, effects, origin);
    }

    private Vector2 GetGripOrigin() => new(8f, 48f);
}
