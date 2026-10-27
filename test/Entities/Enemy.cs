using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace test.Entities;

public enum EnemyStyle
{
    Swordsman,
    Spearman,
    Duelist
}

public sealed class Enemy
{
    private readonly Rectangle _spawnBounds;
    private readonly float _postureBreakDuration;
    private readonly float _moveSpeed;
    private readonly float _weaponIdleAngle;
    private readonly int _frameHeight;
    private readonly Color _tint;
    private readonly IReadOnlyList<EnemyAttackDefinition> _attacks;
    private IEnemyAi _ai;
    private int _nextAttackIndex;
    private float _attackVisualTimer;

    public Rectangle Bounds { get; set; }
    public int MaxHealth { get; }
    public int Health { get; private set; }
    public EnemyStyle Style { get; }
    public float MaxPosture { get; }
    public float Posture { get; set; }
    public int PositionX => Bounds.X;
    public bool IsAlive => Health > 0;
    public bool FacingRight { get; set; }
    public bool IsPreparingAttack => AttackWindupTimer > 0f;
    public bool IsPostureBroken => PostureBreakTimer > 0f;
    public bool AttackImpactPending { get; set; }
    public float MoveSpeed => _moveSpeed;
    public float EngageRange => _attacks.Count == 0 ? 0f : System.Linq.Enumerable.Max(_attacks, attack => attack.TriggerRange);
    public float AttackWindupTimer { get; set; }
    public float AttackCooldownTimer { get; set; }
    public float PostureBreakTimer { get; set; }
    public float HitFlashTimer { get; set; }
    public EnemyAttackDefinition CurrentAttack { get; private set; }
    public float AttackVisualTimer => _attackVisualTimer;
    public IReadOnlyList<EnemyAttackDefinition> Attacks => _attacks;

    public Enemy(Rectangle bounds, EnemyDefinition definition, IEnemyAi ai = null)
    {
        System.ArgumentNullException.ThrowIfNull(definition);
        MaxHealth = definition.MaxHealth;
        Health = definition.MaxHealth;
        Style = definition.Style;
        MaxPosture = definition.MaxPosture;
        _postureBreakDuration = definition.PostureBreakDuration;
        _weaponIdleAngle = definition.WeaponIdleAngle;
        _frameHeight = definition.Height;
        Bounds = new Rectangle(bounds.X, bounds.Y, definition.Width, definition.Height);
        _spawnBounds = Bounds;
        _ai = ai ?? new DefaultEnemyAi();
        _moveSpeed = definition.MoveSpeed;
        _tint = definition.Tint;
        _attacks = definition.Attacks;
        _ai.Reset(this);
    }

    public void SetAi(IEnemyAi ai)
    {
        _ai = ai ?? new DefaultEnemyAi();
        _ai.Reset(this);
    }

    public void AddPostureDamage(float damage)
    {
        if (damage <= 0f || !IsAlive || IsPostureBroken)
        {
            return;
        }

        Posture = System.Math.Min(MaxPosture, Posture + damage);
        if (Posture >= MaxPosture)
        {
            PostureBreakTimer = _postureBreakDuration;
            AttackWindupTimer = 0f;
        }
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || !IsAlive)
        {
            return;
        }

        Health = System.Math.Max(0, Health - damage);
        AddPostureDamage(damage * 0.65f);
        HitFlashTimer = 0.12f;
    }

    public void Reset()
    {
        Bounds = _spawnBounds;
        Health = MaxHealth;
        AttackWindupTimer = 0f;
        AttackCooldownTimer = 0f;
        PostureBreakTimer = 0f;
        HitFlashTimer = 0f;
        Posture = 0f;
        AttackImpactPending = false;
        FacingRight = false;
        CurrentAttack = null;
        _nextAttackIndex = 0;
        _attackVisualTimer = 0f;
        _ai.Reset(this);
    }

    public void RestoreState(int health, int positionX, Rectangle sceneBounds)
    {
        Bounds = new Rectangle(
            MathHelper.Clamp(positionX, sceneBounds.Left, sceneBounds.Right - Bounds.Width),
            Bounds.Y,
            Bounds.Width,
            Bounds.Height);
        Health = System.Math.Clamp(health, 0, MaxHealth);
        AttackWindupTimer = 0f;
        AttackCooldownTimer = 0f;
        PostureBreakTimer = 0f;
        HitFlashTimer = 0f;
        Posture = 0f;
        AttackImpactPending = false;
        CurrentAttack = null;
        _attackVisualTimer = 0f;
    }

    public void Update(GameTime gameTime, Player player, Rectangle sceneBounds)
    {
        var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _attackVisualTimer = System.Math.Max(0f, _attackVisualTimer - elapsedSeconds);
        _ai.Update(new EnemyAiContext(this, player, sceneBounds, elapsedSeconds));
        if (AttackImpactPending)
        {
            _attackVisualTimer = 0.14f;
        }
    }

    public bool TryBeginAttack(float horizontalDistance, float rangeScale, float windupScale, float cooldownScale)
    {
        if (_attacks.Count == 0)
        {
            return false;
        }

        for (var offset = 0; offset < _attacks.Count; offset++)
        {
            var index = (_nextAttackIndex + offset) % _attacks.Count;
            var attack = _attacks[index];
            if (System.Math.Abs(horizontalDistance) > attack.TriggerRange * rangeScale)
            {
                continue;
            }

            CurrentAttack = attack;
            AttackWindupTimer = System.Math.Max(0.08f, attack.WindupDuration * windupScale);
            AttackCooldownTimer = System.Math.Max(0.15f, attack.CooldownDuration * cooldownScale);
            _nextAttackIndex = (index + 1) % _attacks.Count;
            return true;
        }

        return false;
    }

    public bool ResolveAttack(Player player)
    {
        if (!AttackImpactPending || CurrentAttack == null)
        {
            return false;
        }

        AttackImpactPending = false;
        if (!CurrentAttack.Intersects(Bounds, FacingRight, player.Bounds))
        {
            return false;
        }

        var playerFacesEnemy = player.FacingRight == (Bounds.Center.X >= player.Bounds.Center.X);
        if (player.IsParrying && playerFacesEnemy)
        {
            AddPostureDamage(CurrentAttack.ParryPostureDamage);
            player.OnParrySuccess();
            return true;
        }

        player.TakeDamage(CurrentAttack.Damage);
        player.AddPostureDamage(CurrentAttack.PostureDamage);
        return false;
    }

    public bool CanDeathblow(Player player)
    {
        return IsAlive &&
            IsPostureBroken &&
            System.Math.Abs(player.Bounds.Center.X - Bounds.Center.X) <= 96f &&
            Bounds.Bottom > player.Bounds.Top &&
            Bounds.Top < player.Bounds.Bottom;
    }

    public bool TryDeathblow(Player player)
    {
        if (!CanDeathblow(player))
        {
            return false;
        }

        Health = 0;
        return true;
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

        var source = new Rectangle(0, 0, texture.Width, _frameHeight);
        var origin = new Vector2(Bounds.Width / 2f, 0f);
        var effects = FacingRight ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        var color = HitFlashTimer > 0f
            ? Color.White
            : IsPostureBroken ? Color.OrangeRed : IsPreparingAttack ? Color.OrangeRed : _tint;
        spriteBatch.Draw(texture, new Vector2(Bounds.Center.X, Bounds.Y), source, color, 0f, origin, 1f, effects, 0f);

        var weaponAngle = CurrentAttack == null
            ? _weaponIdleAngle
            : IsPreparingAttack
                ? CurrentAttack.WeaponWindupAngle
                : _attackVisualTimer > 0f ? CurrentAttack.WeaponStrikeAngle : _weaponIdleAngle;
        if (!FacingRight)
        {
            weaponAngle = -weaponAngle;
        }

        var grip = CurrentAttack?.WeaponGripOrigin ?? new Vector2(8f, 48f);
        var weaponOrigin = FacingRight
            ? grip
            : new Vector2(weaponTexture.Width - grip.X, grip.Y);
        var weaponEffects = FacingRight
            ? SpriteEffects.None
            : SpriteEffects.FlipHorizontally;
        var handPosition = new Vector2(
            Bounds.Center.X + (FacingRight ? 10f : -10f),
            Bounds.Top + 36f);
        var weaponColor = CurrentAttack?.WeaponKind == EnemyWeaponKind.Spear
            ? Color.LightBlue
            : _tint;
        spriteBatch.Draw(
            weaponTexture,
            handPosition,
            null,
            weaponColor,
            weaponAngle,
            weaponOrigin,
            CurrentAttack?.WeaponScale ?? Vector2.One,
            weaponEffects,
            0f);

        if (IsPreparingAttack && CurrentAttack != null)
        {
            DrawAttackTelegraph(spriteBatch, pixelTexture);
        }

        var postureBar = new Rectangle(Bounds.X, Bounds.Y - 19, Bounds.Width, 5);
        var postureFill = new Rectangle(
            postureBar.X,
            postureBar.Y,
            (int)(postureBar.Width * (Posture / MaxPosture)),
            postureBar.Height);
        spriteBatch.Draw(pixelTexture, postureBar, Color.DarkSlateGray);
        spriteBatch.Draw(pixelTexture, postureFill, IsPostureBroken ? Color.Orange : Color.Gold);

        var healthBar = new Rectangle(Bounds.X, Bounds.Y - 10, Bounds.Width, 5);
        var healthFill = new Rectangle(
            healthBar.X,
            healthBar.Y,
            (int)(healthBar.Width * (Health / (float)MaxHealth)),
            healthBar.Height);
        spriteBatch.Draw(pixelTexture, healthBar, Color.DarkRed);
        spriteBatch.Draw(pixelTexture, healthFill, Color.LimeGreen);
    }

    private void DrawAttackTelegraph(SpriteBatch spriteBatch, Texture2D pixelTexture)
    {
        var bounds = CurrentAttack.GetHitboxBounds(Bounds, FacingRight);
        var progress = 1f - AttackWindupTimer / CurrentAttack.WindupDuration;
        var alpha = (int)(70 + 120 * MathHelper.Clamp(progress, 0f, 1f));
        var color = new Color(255, 75, 35, alpha);
        if (CurrentAttack.HitboxShape == EnemyHitboxShape.Rectangle)
        {
            spriteBatch.Draw(pixelTexture, bounds, color);
            DrawRectangleOutline(spriteBatch, pixelTexture, bounds, Color.OrangeRed);
            return;
        }

        var radius = CurrentAttack.HitboxRadius;
        for (var y = -5; y <= 5; y++)
        {
            var normalizedY = y / 5f;
            var halfWidth = (int)(radius * (float)System.Math.Sqrt(
                System.Math.Max(0f, 1f - normalizedY * normalizedY)));
            if (halfWidth <= 0)
            {
                continue;
            }

            var stripHeight = System.Math.Max(1, (int)(radius / 5f));
            spriteBatch.Draw(
                pixelTexture,
                new Rectangle(bounds.Center.X - halfWidth, bounds.Center.Y + y * stripHeight, halfWidth * 2, stripHeight),
                color);
        }
    }

    private static void DrawRectangleOutline(
        SpriteBatch spriteBatch,
        Texture2D pixelTexture,
        Rectangle bounds,
        Color color)
    {
        spriteBatch.Draw(pixelTexture, new Rectangle(bounds.Left, bounds.Top, bounds.Width, 2), color);
        spriteBatch.Draw(pixelTexture, new Rectangle(bounds.Left, bounds.Bottom - 2, bounds.Width, 2), color);
        spriteBatch.Draw(pixelTexture, new Rectangle(bounds.Left, bounds.Top, 2, bounds.Height), color);
        spriteBatch.Draw(pixelTexture, new Rectangle(bounds.Right - 2, bounds.Top, 2, bounds.Height), color);
    }
}
