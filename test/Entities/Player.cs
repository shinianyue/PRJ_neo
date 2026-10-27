using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace test.Entities
{
    public sealed class Player
    {
        private const int FrameHeight = 58;
        private static readonly int[] WalkFrames = { 1, 2, 3, 4, 5, 6 };
        private static readonly int[] RunFrames = { 7, 8, 9, 10, 11, 12, 13, 14 };
        private static readonly int[] JumpFrames = { 10, 11 };
        private enum AnimationMode { Idle, Walk, Run, Jump }

        private Texture2D _texture;
        private KeyboardState _previousKeyboardState;
        private float _verticalVelocity;
        private float _horizontalVelocity;
        private float _dashVelocity;
        private float _dashTimeRemaining;
        private float _animationTimer;
        private float _parryTimer;
        private float _posture;
        private int _animationFrameIndex;
        private int _animationFrame;
        private AnimationMode _animationMode;
        private Vector2 _position;
        public float walkSpeed = 140f;
        public float runSpeed = 280f;
        public float acceleration = 900f;
        public float deceleration = 1200f;
        public float jumpSpeed = 650f;
        public float gravity = 1100f;
        public bool IsGrounded { get; private set; }
        public bool FacingRight { get; private set; } = true;
        public bool IsParrying { get; private set; }
        public int MaxHealth { get; } = 10000;
        public int Health { get; private set; } = 100;
        public float MaxPosture { get; } = 100f;
        public float Posture => _posture;
        public Vector2 Position => _position;
        public Rectangle Bounds => new Rectangle((int)_position.X, (int)_position.Y, _texture.Width, FrameHeight);
        public Vector2 HandPosition => new(
            FacingRight ? Bounds.Left + _texture.Width - 10 : Bounds.Left + 10,
            Bounds.Top + 36);

        public Player(Texture2D texture)
        {
            _texture = texture;
        }

        public void ResetPosition(Vector2 position)
        {
            _position = position;
            _verticalVelocity = 0f;
            _horizontalVelocity = 0f;
            _dashVelocity = 0f;
            _dashTimeRemaining = 0f;
            _animationTimer = 0f;
            _parryTimer = 0f;
            _posture = 0f;
            _animationFrameIndex = 0;
            _animationFrame = 0;
            _animationMode = AnimationMode.Idle;
            IsGrounded = false;
            FacingRight = true;
            IsParrying = false;
            _previousKeyboardState = Keyboard.GetState();
        }

        public void SetHorizontalPosition(float x)
        {
            _position.X = x;
        }

        public void StartDash(float distance, float duration)
        {
            if (distance <= 0f || duration <= 0f)
            {
                return;
            }

            _dashVelocity = (FacingRight ? 1f : -1f) * distance / duration;
            _dashTimeRemaining = duration;
            _horizontalVelocity = _dashVelocity;
        }

        public void TakeDamage(int damage)
        {
            if (damage > 0)
            {
                Health = Math.Max(0, Health - damage);
            }
        }

        public void AddPostureDamage(float damage)
        {
            if (damage > 0f)
            {
                _posture = Math.Min(MaxPosture, _posture + damage);
            }
        }

        public void OnParrySuccess()
        {
            IsParrying = false;
            _parryTimer = 0f;
            _posture = Math.Max(0f, _posture - 24f);
        }

        public void ResetHealth()
        {
            Health = MaxHealth;
        }

        public void RestoreState(Vector2 position, int health)
        {
            ResetPosition(position);
            Health = Math.Clamp(health, 1, MaxHealth);
        }

        public void Update(GameTime gameTime, IReadOnlyList<Rectangle> platforms, Rectangle bounds)
        {
            var keyboardState = Keyboard.GetState();
            var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var horizontalInput =
                (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right) ? 1f : 0f) -
                (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left) ? 1f : 0f);
            var isRunning = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
            var parryPressed =
                (keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl) ||
                 keyboardState.IsKeyDown(Keys.K)) &&
                !(_previousKeyboardState.IsKeyDown(Keys.LeftControl) ||
                  _previousKeyboardState.IsKeyDown(Keys.RightControl) ||
                  _previousKeyboardState.IsKeyDown(Keys.K));
            if (parryPressed && IsGrounded && _parryTimer <= 0f)
            {
                _parryTimer = 0.20f;
            }
            else if (_parryTimer > 0f)
            {
                _parryTimer = Math.Max(0f, _parryTimer - elapsedSeconds);
            }
            IsParrying = _parryTimer > 0f;
            _posture = Math.Max(0f, _posture - 12f * elapsedSeconds);

            if (_dashTimeRemaining > 0f)
            {
                _position.X += _dashVelocity * elapsedSeconds;
                _dashTimeRemaining = Math.Max(0f, _dashTimeRemaining - elapsedSeconds);
                if (_dashTimeRemaining == 0f)
                {
                    _dashVelocity = 0f;
                    _horizontalVelocity = 0f;
                }
            }
            else
            {
                var targetVelocity = horizontalInput * (isRunning ? runSpeed : walkSpeed);
                var rate = horizontalInput == 0f ? deceleration : acceleration;
                _horizontalVelocity = MoveTowards(_horizontalVelocity, targetVelocity, rate * elapsedSeconds);
                _position.X += _horizontalVelocity * elapsedSeconds;
            }

            _position.X = MathHelper.Clamp(_position.X, bounds.Left, bounds.Right - _texture.Width);
            if (_position.X <= bounds.Left || _position.X >= bounds.Right - _texture.Width)
            {
                _horizontalVelocity = 0f;
                _dashVelocity = 0f;
                _dashTimeRemaining = 0f;
            }
            if (horizontalInput != 0f)
            {
                FacingRight = horizontalInput > 0f;
            }

            var jumpPressed =
                keyboardState.IsKeyDown(Keys.Space) ||
                keyboardState.IsKeyDown(Keys.W) ||
                keyboardState.IsKeyDown(Keys.Up);
            var wasJumpPressed =
                _previousKeyboardState.IsKeyDown(Keys.Space) ||
                _previousKeyboardState.IsKeyDown(Keys.W) ||
                _previousKeyboardState.IsKeyDown(Keys.Up);
            if (IsGrounded && jumpPressed && !wasJumpPressed)
            {
                _verticalVelocity = -jumpSpeed;
                IsGrounded = false;
            }

            var previousBottom = _position.Y + FrameHeight;
            _verticalVelocity += gravity * elapsedSeconds;
            _position.Y += _verticalVelocity * elapsedSeconds;
            IsGrounded = false;

            if (_verticalVelocity >= 0f)
            {
                var currentBounds = new Rectangle(
                    (int)_position.X,
                    (int)_position.Y,
                    _texture.Width,
                    FrameHeight);
                foreach (var platform in platforms)
                {
                    if (previousBottom <= platform.Top &&
                        _position.Y + FrameHeight >= platform.Top &&
                        currentBounds.Right > platform.Left &&
                        currentBounds.Left < platform.Right)
                    {
                        _position.Y = platform.Top - FrameHeight;
                        _verticalVelocity = 0f;
                        IsGrounded = true;
                        break;
                    }
                }
            }

            if (_position.Y < bounds.Top)
            {
                _position.Y = bounds.Top;
                _verticalVelocity = 0f;
            }
            if (_position.Y + FrameHeight > bounds.Bottom)
            {
                _position.Y = bounds.Bottom - FrameHeight;
                _verticalVelocity = 0f;
                IsGrounded = true;
            }

            UpdateAnimation(elapsedSeconds, isRunning);
            _previousKeyboardState = keyboardState;
        }

        private void UpdateAnimation(float elapsedSeconds, bool isRunning)
        {
            if (IsParrying)
            {
                _animationMode = AnimationMode.Idle;
                _animationFrame = 0;
                _animationTimer = 0f;
                _animationFrameIndex = 0;
                return;
            }

            if (!IsGrounded)
            {
                Animate(JumpFrames, AnimationMode.Jump, elapsedSeconds, 0.14f);
                return;
            }

            if (Math.Abs(_horizontalVelocity) < 1f)
            {
                _animationMode = AnimationMode.Idle;
                _animationFrame = 0;
                _animationTimer = 0f;
                _animationFrameIndex = 0;
                return;
            }

            var frames = isRunning ? RunFrames : WalkFrames;
            var mode = isRunning ? AnimationMode.Run : AnimationMode.Walk;
            Animate(frames, mode, elapsedSeconds, isRunning ? 0.07f : 0.12f);
        }

        private void Animate(int[] frames, AnimationMode mode, float elapsedSeconds, float frameDuration)
        {
            if (_animationMode != mode)
            {
                _animationMode = mode;
                _animationFrameIndex = 0;
                _animationFrame = frames[0];
                _animationTimer = 0f;
                return;
            }

            _animationTimer += elapsedSeconds;
            while (_animationTimer >= frameDuration)
            {
                _animationTimer -= frameDuration;
                _animationFrameIndex = (_animationFrameIndex + 1) % frames.Length;
            }
            _animationFrame = frames[_animationFrameIndex];
        }

        private static float MoveTowards(float current, float target, float maxDelta)
        {
            if (Math.Abs(target - current) <= maxDelta)
            {
                return target;
            }

            return current + Math.Sign(target - current) * maxDelta;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            var source = new Rectangle(0, _animationFrame * FrameHeight, _texture.Width, FrameHeight);
            var effects = FacingRight ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            var origin = new Vector2(_texture.Width / 2f, 0f);
            var drawPosition = _position + new Vector2(origin.X, 0f);
            var color = IsParrying ? Color.LightGoldenrodYellow : Color.White;
            spriteBatch.Draw(_texture, drawPosition, source, color, 0f, origin, 1f, effects, 0f);
        }
    }
}