using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace test.Entities;

public sealed class Enemy
{
    private const int FrameHeight = 58;
    private const float MoveSpeed = 75f;
    private const float AttackRange = 52f;
    private const float AttackWindup = 0.28f;
    private const float AttackCooldown = 1.2f;
    private const int AttackDamage = 20;
    private readonly Rectangle _spawnBounds;
    private float _attackWindupTimer;
    private float _attackCooldownTimer;

    public Rectangle Bounds { get; private set; }
    public int MaxHealth { get; }
    public int Health { get; private set; }
    public int PositionX => Bounds.X;
    public bool IsAlive => Health > 0;
    public bool FacingRight { get; private set; }
    public bool IsPreparingAttack => _attackWindupTimer > 0f;

    public Enemy(Rectangle bounds, int maxHealth)
    {
        _spawnBounds = bounds;
        Bounds = bounds;
        MaxHealth = maxHealth;
        Health = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || !IsAlive)
        {
            return;
        }

        Health = System.Math.Max(0, Health - damage);
    }

    public void Reset()
    {
        Bounds = _spawnBounds;
        Health = MaxHealth;
        _attackWindupTimer = 0f;
        _attackCooldownTimer = 0f;
        FacingRight = false;
    }

    public void RestoreState(int health, int positionX, Rectangle sceneBounds)
    {
        Bounds = new Rectangle(
            MathHelper.Clamp(positionX, sceneBounds.Left, sceneBounds.Right - Bounds.Width),
            Bounds.Y,
            Bounds.Width,
            Bounds.Height);
        Health = System.Math.Clamp(health, 0, MaxHealth);
        _attackWindupTimer = 0f;
        _attackCooldownTimer = 0f;
    }

    public void Update(GameTime gameTime, Player player, Rectangle sceneBounds)
    {
        if (!IsAlive)
        {
            return;
        }

        var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _attackCooldownTimer = System.Math.Max(0f, _attackCooldownTimer - elapsedSeconds);
        var horizontalDistance = player.Bounds.Center.X - Bounds.Center.X;
        var verticallyAligned = Bounds.Bottom > player.Bounds.Top && Bounds.Top < player.Bounds.Bottom;
        FacingRight = horizontalDistance > 0;

        if (_attackWindupTimer > 0f)
        {
            _attackWindupTimer -= elapsedSeconds;
            if (_attackWindupTimer <= 0f &&
                System.Math.Abs(horizontalDistance) <= AttackRange &&
                verticallyAligned)
            {
                player.TakeDamage(AttackDamage);
            }
            return;
        }

        if (System.Math.Abs(horizontalDistance) <= AttackRange &&
            verticallyAligned &&
            _attackCooldownTimer <= 0f)
        {
            _attackWindupTimer = AttackWindup;
            _attackCooldownTimer = AttackCooldown;
            return;
        }

        if (System.Math.Abs(horizontalDistance) > AttackRange)
        {
            var direction = System.Math.Sign(horizontalDistance);
            var nextX = Bounds.X + direction * MoveSpeed * elapsedSeconds;
            nextX = MathHelper.Clamp(nextX, sceneBounds.Left, sceneBounds.Right - Bounds.Width);
            Bounds = new Rectangle((int)nextX, Bounds.Y, Bounds.Width, Bounds.Height);
        }
    }

    public void Draw(
        SpriteBatch spriteBatch,
        Texture2D texture,
        Texture2D weaponTexture,
        Texture2D pixelTexture)
    {
        if (!IsAlive)
        {
            return;
        }

        var source = new Rectangle(0, 0, texture.Width, FrameHeight);
        var origin = new Vector2(Bounds.Width / 2f, 0f);
        var effects = FacingRight ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        var color = IsPreparingAttack ? Color.OrangeRed : Color.White;
        spriteBatch.Draw(texture, new Vector2(Bounds.Center.X, Bounds.Y), source, color, 0f, origin, 1f, effects, 0f);

        var weaponAngle = IsPreparingAttack ? -1.1f : -0.35f;
        if (!FacingRight)
        {
            weaponAngle = -weaponAngle;
        }

        var grip = new Vector2(8f, 48f);
        var weaponOrigin = FacingRight
            ? grip
            : new Vector2(weaponTexture.Width - grip.X, grip.Y);
        var handPosition = new Vector2(
            Bounds.Center.X + (FacingRight ? 10f : -10f),
            Bounds.Top + 36f);
        spriteBatch.Draw(
            weaponTexture,
            handPosition,
            null,
            Color.White,
            weaponAngle,
            weaponOrigin,
            1f,
            effects,
            0f);

        var healthBar = new Rectangle(Bounds.X, Bounds.Y - 10, Bounds.Width, 5);
        var healthFill = new Rectangle(
            healthBar.X,
            healthBar.Y,
            (int)(healthBar.Width * (Health / (float)MaxHealth)),
            healthBar.Height);
        spriteBatch.Draw(pixelTexture, healthBar, Color.DarkRed);
        spriteBatch.Draw(pixelTexture, healthFill, Color.LimeGreen);
    }
}
