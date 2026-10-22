using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;
using System.Text.Json;
using test.Combat;
using test.Combat.HitDetection;
using test.Entities;
using test.Persistence;
using test.Story;
using test.UI;
using test.World;

namespace test;

public class Game1 : Game
{
    private enum GameState
    {
        MainMenu,
        Story,
        Playing
    }

    private const int SceneEntryInset = 32;

    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Player _player;
    private Texture2D _wallTexture;
    private Texture2D _sceneBackgroundTexture;
    private Texture2D _obstacleTexture;
    private Texture2D _anglerTexture;
    private Texture2D _weaponTexture;
    private Texture2D _pixelTexture;
    private Weapon _weapon;
    private Scene[] _scenes;
    private int _activeSceneIndex;
    private Camera2D _camera;
    private Texture2D _menuBackground;
    private SpriteFont _menuFont;
    private MainMenu _mainMenu;
    private KeyboardState _previousGameKeyboard;
    private GameState _gameState = GameState.MainMenu;
    private SaveManager _saveManager;
    private StoryPlayer _storyPlayer;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
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
        _sceneBackgroundTexture = Content.Load<Texture2D>("images/Other_Images/Backgrounds/Underworld 3");
        _obstacleTexture = Content.Load<Texture2D>("images/shrubs");
        _menuBackground = Content.Load<Texture2D>("images/img_maininterface(oldone)");
        _menuFont = Content.Load<SpriteFont>("Fonts/Menu");
        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
        _mainMenu = new MainMenu(_menuBackground, _pixelTexture, _menuFont);
        _mainMenu.ResetInput();
        var savePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProjectSny",
            "save.json");
        _saveManager = new SaveManager(savePath);
        _storyPlayer = new StoryPlayer(
            Path.Combine(AppContext.BaseDirectory, "Content", "Stories"),
            _pixelTexture,
            _menuFont);

        _player = new Player(_anglerTexture);
        var swordAttacks = new[]
        {
            new AttackDefinition(
                Keys.J,
                damage: 30,
                duration: 0.30f,
                cooldown: 0.30f,
                activeStart: 0.05f,
                activeEnd: 0.22f,
                startAngle: -1.2f,
                endAngle: 0.8f,
                dashDistance: 0f,
                hitRule: new PixelPerfectHitRule()),
            new AttackDefinition(
                Keys.J,
                damage: 35,
                duration: 0.24f,
                cooldown: 0.26f,
                activeStart: 0.04f,
                activeEnd: 0.18f,
                startAngle: -0.12f,
                endAngle: 0.12f,
                dashDistance: 0f,
                hitRule: new BoundsHitRule()),
            new AttackDefinition(
                Keys.J,
                damage: 50,
                duration: 0.42f,
                cooldown: 0.42f,
                activeStart: 0.10f,
                activeEnd: 0.36f,
                startAngle: -0.5f,
                endAngle: 0.3f,
                dashDistance: 150f,
                hitRule: new PixelPerfectHitRule())
        };
        var swordDefinition = new WeaponDefinition(
            _weaponTexture,
            gripOrigin: new Vector2(8f, 48f),
            idleAngle: -0.35f,
            attacks: swordAttacks,
            comboResetDelay: 1.0f);
        _weapon = new Weapon(swordDefinition);
        _camera = new Camera2D();
        BuildScenes(GraphicsDevice.Viewport);
        RestartGame();
    }

    private void BuildScenes(Viewport viewport)
    {
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
        var guideNpc = new DialogueNpc(
            "Guard",
            new Rectangle(viewport.Width / 5, ground.Top - 58, _anglerTexture.Width, 58),
            "tutorial");
        guideNpc.SetActive(true);

        _scenes = new[]
        {
            new Scene(
                sceneBounds,
                playerStart,
                new[] { secondFloor, ground },
                System.Array.Empty<Rectangle>(),
                null,
                guideNpc),
            new Scene(
                sceneBounds,
                playerStart,
                new[] { secondFloor, ground },
                new[] { obstacleBounds },
                enemy)
        };
    }

    protected override void Update(GameTime gameTime)
    {
        if (_gameState == GameState.MainMenu)
        {
            var action = _mainMenu.Update(GraphicsDevice.Viewport.Bounds, _saveManager.HasSave);
            if (action == MenuAction.StartGame)
            {
                StartNewGame();
            }
            else if (action == MenuAction.ContinueGame)
            {
                ContinueGame();
            }
            else if (action == MenuAction.Exit)
            {
                Exit();
            }
            else if (action == MenuAction.ApplyResolution)
            {
                var resolution = _mainMenu.SelectedResolution;
                if (GraphicsDevice.Viewport.Width != resolution.X ||
                    GraphicsDevice.Viewport.Height != resolution.Y)
                {
                    _graphics.PreferredBackBufferWidth = resolution.X;
                    _graphics.PreferredBackBufferHeight = resolution.Y;
                    _graphics.ApplyChanges();
                    BuildScenes(GraphicsDevice.Viewport);
                    RestartGame();
                    _mainMenu.ResetInput();
                }
            }

            base.Update(gameTime);
            return;
        }

        if (_gameState == GameState.Story)
        {
            if (_storyPlayer.Update())
            {
                _gameState = GameState.Playing;
                SaveGame();
            }

            base.Update(gameTime);
            return;
        }

        var keyboardState = Keyboard.GetState();
        var scene = _scenes[_activeSceneIndex];
        var interactPressed = keyboardState.IsKeyDown(Keys.E) && _previousGameKeyboard.IsKeyUp(Keys.E);
        if (interactPressed && scene.Npc != null && scene.Npc.IsPlayerNearby(_player))
        {
            _storyPlayer.Start(scene.Npc.SequenceId);
            _gameState = GameState.Story;
            _previousGameKeyboard = keyboardState;
            base.Update(gameTime);
            return;
        }

        if (keyboardState.IsKeyDown(Keys.Escape) && _previousGameKeyboard.IsKeyUp(Keys.Escape))
        {
            SaveGame();
            _gameState = GameState.MainMenu;
            _previousGameKeyboard = keyboardState;
            _mainMenu.ResetInput();
            base.Update(gameTime);
            return;
        }

        if (keyboardState.IsKeyDown(Keys.F5) && _previousGameKeyboard.IsKeyUp(Keys.F5))
        {
            SaveGame();
        }

        var sceneIndexBeforeUpdate = _activeSceneIndex;
        _player.Update(gameTime, scene.Platforms, scene.Bounds);
        _weapon.Update(gameTime, _player);
        if (scene.Enemy != null)
        {
            scene.Enemy.Update(gameTime, _player, scene.Bounds);
            var canParry = keyboardState.IsKeyDown(Keys.L) || keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
            if (canParry && _player.IsParrying && scene.Enemy.IsPreparingAttack)
            {
                var enemyHitBox = scene.Enemy.Bounds;
                if (Math.Abs(_player.Bounds.Center.X - enemyHitBox.Center.X) < 90f)
                {
                    scene.Enemy.TakeDamage(30);
                    _player.OnParrySuccess();
                }
            }

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

        if (_activeSceneIndex != sceneIndexBeforeUpdate)
        {
            SaveGame();
        }

        _camera.Follow(_player, _scenes[_activeSceneIndex].Bounds, GraphicsDevice.Viewport);
        _previousGameKeyboard = keyboardState;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        if (_gameState == GameState.MainMenu)
        {
            _spriteBatch.Begin();
            _mainMenu.Draw(_spriteBatch, GraphicsDevice.Viewport.Bounds, _saveManager.HasSave);
            _spriteBatch.End();
            base.Draw(gameTime);
            return;
        }

        _spriteBatch.Begin(transformMatrix: _camera.BackgroundTransform);
        _scenes[_activeSceneIndex].DrawBackground(_spriteBatch, _sceneBackgroundTexture);
        _spriteBatch.End();

        _spriteBatch.Begin(transformMatrix: _camera.Transform);
        _scenes[_activeSceneIndex].Draw(
            _spriteBatch,
            _wallTexture,
            _obstacleTexture,
            _anglerTexture,
            _weaponTexture,
            _pixelTexture);
        _player.Draw(_spriteBatch);
        _weapon.Draw(_spriteBatch, _player);
        _spriteBatch.End();

        _spriteBatch.Begin();
        DrawPlayerHealth();
        DrawInteractionHint();
        _storyPlayer.Draw(_spriteBatch, GraphicsDevice.Viewport.Bounds);
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

    private void StartNewGame()
    {
        RestartGame();
        SaveGame();
        _gameState = GameState.Playing;
        _mainMenu.SetStatusMessage(string.Empty);
    }

    private void ContinueGame()
    {
        if (!_saveManager.HasSave)
        {
            _mainMenu.SetStatusMessage("No save found. Choose NEW GAME.");
            return;
        }

        try
        {
            var save = _saveManager.Load();
            if (save.SceneIndex >= _scenes.Length)
            {
                throw new InvalidDataException("存档中的场景编号无效。");
            }

            _activeSceneIndex = save.SceneIndex;
            foreach (var scene in _scenes)
            {
                scene.Enemy?.Reset();
            }

            var sceneBounds = _scenes[_activeSceneIndex].Bounds;
            var playerX = MathHelper.Clamp(
                save.PlayerX,
                sceneBounds.Left,
                sceneBounds.Right - _player.Bounds.Width);
            var playerY = MathHelper.Clamp(
                save.PlayerY,
                sceneBounds.Top,
                sceneBounds.Bottom - _player.Bounds.Height);
            _player.RestoreState(new Vector2(playerX, playerY), save.PlayerHealth);
            _weapon.Reset();

            var enemy = _scenes[1].Enemy;
            enemy?.RestoreState(save.EnemyHealth, save.EnemyX, _scenes[1].Bounds);
            _camera.Follow(_player, sceneBounds, GraphicsDevice.Viewport);
            _gameState = GameState.Playing;
            _mainMenu.SetStatusMessage(string.Empty);
        }
        catch (IOException exception)
        {
            _mainMenu.SetStatusMessage($"Load failed: {exception.Message}");
        }
        catch (InvalidDataException exception)
        {
            _mainMenu.SetStatusMessage($"Save data is invalid: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            _mainMenu.SetStatusMessage($"Cannot read save: {exception.Message}");
        }
        catch (JsonException exception)
        {
            _mainMenu.SetStatusMessage($"Save data is invalid: {exception.Message}");
        }
    }

    private void SaveGame()
    {
        try
        {
            var enemy = _scenes[1].Enemy;
            _saveManager.Save(new SaveData
            {
                SceneIndex = _activeSceneIndex,
                PlayerX = _player.Position.X,
                PlayerY = _player.Position.Y,
                PlayerHealth = _player.Health,
                EnemyHealth = enemy?.Health ?? 0,
                EnemyX = enemy?.PositionX ?? 0
            });
            _mainMenu.SetStatusMessage("Game saved.");
        }
        catch (IOException exception)
        {
            _mainMenu.SetStatusMessage($"Save failed: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            _mainMenu.SetStatusMessage($"Cannot write save: {exception.Message}");
        }
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

    private void DrawInteractionHint()
    {
        var npc = _scenes[_activeSceneIndex].Npc;
        if (npc == null || !npc.IsPlayerNearby(_player))
        {
            return;
        }

        const string hint = "E - TALK";
        var size = _menuFont.MeasureString(hint);
        _spriteBatch.DrawString(
            _menuFont,
            hint,
            new Vector2(
                GraphicsDevice.Viewport.Width / 2f - size.X / 2f,
                GraphicsDevice.Viewport.Height - size.Y - 24f),
            Color.Gold);
    }
}
