using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using test.Combat;
using test.Entities;
using test.World;

namespace test;

public class Game1 : Game
{
    private const int SceneEntryInset = 32;

    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Player _player;
    private Texture2D _wallTexture;
    private Texture2D _obstacleTexture;
    private Texture2D _anglerTexture;
    private Texture2D _weaponTexture;
    private Texture2D _pixelTexture;
    private Weapon _weapon;
    private Scene[] _scenes;
    private int _activeSceneIndex;
    private Camera2D _camera;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _anglerTexture = Content.Load<Texture2D>("images/Angler_Default");
        _weaponTexture = Content.Load<Texture2D>("images/Item_155");
        _wallTexture = Content.Load<Texture2D>("images/brick");
        _obstacleTexture = Content.Load<Texture2D>("images/shrubs");
        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        var viewport = GraphicsDevice.Viewport;
        var sceneWidth = viewport.Width * 2;
        var ground = new Rectangle(0, viewport.Height - _wallTexture.Height, sceneWidth, _wallTexture.Height);
        var secondFloor = new Rectangle(
            viewport.Width / 4,
            (int)(viewport.Height * 0.6f),
            viewport.Width / 2,
            _wallTexture.Height);

        var sceneBounds = new Rectangle(0, 0, sceneWidth, viewport.Height);
        var playerStart = new Vector2(32f, ground.Top - 58);
        var obstacleBounds = new Rectangle(
            (int)(viewport.Width * 0.65f),
            ground.Top - _obstacleTexture.Height,
            _obstacleTexture.Width,
            _obstacleTexture.Height);
        var enemy = new Enemy(
            new Rectangle((int)(viewport.Width * 0.45f), ground.Top - 58, 40, 58),
            200);

        _scenes = new[]
        {
            new Scene(
                sceneBounds,
                playerStart,
                new[] { secondFloor, ground },
                System.Array.Empty<Rectangle>(),
                null),
            new Scene(
                sceneBounds,
                playerStart,
                new[] { secondFloor, ground },
                new[] { obstacleBounds },
                enemy)
        };

        _player = new Player(_anglerTexture);
        _weapon = new Weapon(_weaponTexture);
        _camera = new Camera2D();
        RestartGame();
    }

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        var scene = _scenes[_activeSceneIndex];
        _player.Update(gameTime, scene.Platforms, scene.Bounds);
        _weapon.Update(gameTime, _player);
        if (scene.Enemy != null)
        {
            scene.Enemy.Update(gameTime, _player, scene.Bounds);
            _weapon.TryHit(scene.Enemy, _player);
            if (_player.Health <= 0)
            {
                RestartGame();
                return;
            }
        }

        foreach (var obstacle in scene.Obstacles)
        {
            if (_player.Bounds.Intersects(obstacle))
            {
                RestartGame();
                break;
            }
        }

        if (_activeSceneIndex == 0 && _player.Bounds.Right >= scene.Bounds.Right)
        {
            _activeSceneIndex = 1;
            _player.SetHorizontalPosition(_scenes[_activeSceneIndex].Bounds.Left + SceneEntryInset);
        }
        else if (_activeSceneIndex == 1 && _player.Bounds.Left <= scene.Bounds.Left)
        {
            _activeSceneIndex = 0;
            _player.SetHorizontalPosition(
                _scenes[_activeSceneIndex].Bounds.Right - _player.Bounds.Width - SceneEntryInset);
        }

        _camera.Follow(_player, _scenes[_activeSceneIndex].Bounds, GraphicsDevice.Viewport);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin(transformMatrix: _camera.Transform);
        _scenes[_activeSceneIndex].Draw(
            _spriteBatch,
            _wallTexture,
            _obstacleTexture,
            _anglerTexture,
            _pixelTexture);
        _player.Draw(_spriteBatch);
        _weapon.Draw(_spriteBatch, _player);
        _spriteBatch.End();

        _spriteBatch.Begin();
        DrawPlayerHealth();
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void RestartGame()
    {
        _activeSceneIndex = 0;
        _player.ResetPosition(_scenes[_activeSceneIndex].PlayerStart);
        _player.ResetHealth();
        _weapon.Reset();
        foreach (var scene in _scenes)
        {
            scene.Enemy?.Reset();
        }
        _camera.Follow(_player, _scenes[_activeSceneIndex].Bounds, GraphicsDevice.Viewport);
    }

    private void DrawPlayerHealth()
    {
        var background = new Rectangle(16, 16, 200, 16);
        var fill = new Rectangle(
            background.X,
            background.Y,
            background.Width * _player.Health / _player.MaxHealth,
            background.Height);
        _spriteBatch.Draw(_pixelTexture, background, Color.DarkRed);
        _spriteBatch.Draw(_pixelTexture, fill, Color.LimeGreen);
    }
}
