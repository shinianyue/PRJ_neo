using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using test.Configuration;
using test.Combat;
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
        Playing,
        Victory
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
    private GameConfiguration _gameConfiguration;
    private Weapon _weapon;
    private CombatEffects _combatEffects;
    private Scene[] _scenes;
    private int _activeSceneIndex;
    private Camera2D _camera;
    private Texture2D _menuBackground;
    private SpriteFont _menuFont;
    private MainMenu _mainMenu;
    private KeyboardState _previousGameKeyboard;
    private GameState _gameState = GameState.MainMenu;
    private SaveManager _saveManager;
    private SaveSlotInfo[] _saveSlots;
    private GameSettings _settings;
    private StoryPlayer _storyPlayer;
    private CutsceneOverlay _cutsceneOverlay;
    private int _activeSaveSlot = 1;
    private string _settingsLoadError;

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
        var savePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProjectSny",
            "save.json");
        _saveManager = new SaveManager(savePath);
        _saveSlots = _saveManager.GetSlots();
        try
        {
            _settings = _saveManager.LoadSettings();
        }
        catch (IOException exception)
        {
            _settings = new GameSettings();
            _settingsLoadError = $"Settings could not be loaded: {exception.Message}";
        }
        catch (UnauthorizedAccessException exception)
        {
            _settings = new GameSettings();
            _settingsLoadError = $"Settings could not be read: {exception.Message}";
        }
        catch (InvalidDataException exception)
        {
            _settings = new GameSettings();
            _settingsLoadError = $"Settings data is invalid: {exception.Message}";
        }
        catch (JsonException exception)
        {
            _settings = new GameSettings();
            _settingsLoadError = $"Settings data is invalid: {exception.Message}";
        }

        var initialResolution = MainMenu.GetResolution(_settings.ResolutionIndex);
        _graphics.PreferredBackBufferWidth = initialResolution.X;
        _graphics.PreferredBackBufferHeight = initialResolution.Y;
        _graphics.IsFullScreen = _settings.Fullscreen;
        _graphics.SynchronizeWithVerticalRetrace = _settings.VSync;
        _graphics.ApplyChanges();

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
        _mainMenu = new MainMenu(_menuBackground, _pixelTexture, _anglerTexture, _menuFont, _settings);
        _gameConfiguration = GameConfiguration.Load(
            Path.Combine(AppContext.BaseDirectory, "Data", "game.json"));
        _mainMenu.SetSceneNames(_gameConfiguration.Scenes.Select(scene => scene.Name).ToArray());
        if (!string.IsNullOrEmpty(_settingsLoadError))
        {
            _mainMenu.SetStatusMessage(_settingsLoadError);
        }
        _mainMenu.ResetInput();
        _storyPlayer = new StoryPlayer(
            Path.Combine(AppContext.BaseDirectory, "Content", "Stories"),
            _pixelTexture,
            _menuFont);
        var missingSequence = _gameConfiguration.Scenes
            .Where(scene => scene.Npc != null)
            .Select(scene => scene.Npc.SequenceId)
            .FirstOrDefault(sequenceId => !_storyPlayer.ContainsSequence(sequenceId));
        if (missingSequence != null)
        {
            throw new InvalidDataException($"Scene NPC references missing story sequence '{missingSequence}'.");
        }

        _cutsceneOverlay = new CutsceneOverlay(_pixelTexture, _menuFont);
        _cutsceneOverlay.ReduceMotion = _settings.ReduceMotion;

        _player = new Player(_anglerTexture);
        _weapon = new Weapon(_gameConfiguration.CreateWeapon(_weaponTexture));
        _combatEffects = new CombatEffects();
        _combatEffects.ReduceMotion = _settings.ReduceMotion;
        _camera = new Camera2D();
        BuildScenes(GraphicsDevice.Viewport);
        RestartGame();
    }

    private void BuildScenes(Viewport viewport)
    {
        _scenes = new SceneFactory(
            _gameConfiguration,
            _wallTexture,
            _obstacleTexture,
            _anglerTexture).Build(viewport).ToArray();
    }

    protected override void Update(GameTime gameTime)
    {
        _combatEffects?.Update(gameTime);
        _cutsceneOverlay?.Update(gameTime);
        if (_gameState == GameState.MainMenu)
        {
            var action = _mainMenu.Update(gameTime, GraphicsDevice.Viewport.Bounds, _saveSlots);
            if (action == MenuAction.NewGameSlot)
            {
                StartNewGame(_mainMenu.SelectedSlot);
            }
            else if (action == MenuAction.LoadSlot)
            {
                ContinueGame(_mainMenu.SelectedSlot);
            }
            else if (action == MenuAction.DeleteSlot)
            {
                DeleteSave(_mainMenu.SelectedSlot);
            }
            else if (action == MenuAction.ApplySettings)
            {
                ApplySettings();
            }
            else if (action == MenuAction.Exit)
            {
                Exit();
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

        if (_gameState == GameState.Victory)
        {
            var victoryKeyboard = Keyboard.GetState();
            if ((victoryKeyboard.IsKeyDown(Keys.Enter) && _previousGameKeyboard.IsKeyUp(Keys.Enter)) ||
                (victoryKeyboard.IsKeyDown(Keys.Escape) && _previousGameKeyboard.IsKeyUp(Keys.Escape)))
            {
                _gameState = GameState.MainMenu;
                _mainMenu.ResetInput();
            }

            _previousGameKeyboard = victoryKeyboard;
            base.Update(gameTime);
            return;
        }

        var keyboardState = Keyboard.GetState();
        var scene = _scenes[_activeSceneIndex];
        var interactPressed = keyboardState.IsKeyDown(Keys.E) && _previousGameKeyboard.IsKeyUp(Keys.E);
        if (interactPressed && scene.Npc != null && scene.Npc.IsPlayerNearby(_player))
        {
            _storyPlayer.Start(scene.Npc.SequenceId);
            _cutsceneOverlay.Play("Night Patrol", "Ward of the Hollow", 2.2f);
            _gameState = GameState.Story;
            _previousGameKeyboard = keyboardState;
            base.Update(gameTime);
            return;
        }

        var deathblowTarget = interactPressed
            ? scene.Enemies.FirstOrDefault(enemy => enemy.CanDeathblow(_player))
            : null;
        if (deathblowTarget != null && deathblowTarget.TryDeathblow(_player))
        {
            _cutsceneOverlay.Play("Finisher", "The blade cuts through the silence", 1.8f);
            _combatEffects.AddParrySpark(new Vector2(deathblowTarget.Bounds.Center.X, deathblowTarget.Bounds.Center.Y));
            _gameState = _scenes.SelectMany(currentScene => currentScene.Enemies).All(enemy => !enemy.IsAlive)
                ? GameState.Victory
                : GameState.Playing;
            SaveGame();
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
        if (scene.Enemies.Count > 0)
        {
            foreach (var enemy in scene.Enemies)
            {
                enemy.Update(gameTime, _player, scene.Bounds);
                if (enemy.ResolveAttack(_player))
                {
                    _combatEffects.AddParrySpark(new Vector2(
                        (enemy.Bounds.Center.X + _player.Bounds.Center.X) / 2f,
                        (enemy.Bounds.Center.Y + _player.Bounds.Center.Y) / 2f));
                }

                if (_weapon.TryHit(enemy, _player))
                {
                    _combatEffects.AddHitSpark(
                        new Vector2(enemy.Bounds.Center.X, enemy.Bounds.Center.Y - 6f),
                        Color.Orange);
                }
            }

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

        if (_activeSceneIndex < _scenes.Length - 1 && _player.Bounds.Right >= scene.Bounds.Right)
        {
            _activeSceneIndex++;
            var nextScene = _gameConfiguration.Scenes[_activeSceneIndex];
            _cutsceneOverlay.Play(nextScene.Name, nextScene.TransitionSubtitle, 2.0f);
            _player.SetHorizontalPosition(_scenes[_activeSceneIndex].Bounds.Left + SceneEntryInset);
        }
        else if (_activeSceneIndex > 0 && _player.Bounds.Left <= scene.Bounds.Left)
        {
            _activeSceneIndex--;
            var previousScene = _gameConfiguration.Scenes[_activeSceneIndex];
            _cutsceneOverlay.Play(previousScene.Name, previousScene.TransitionSubtitle, 2.0f);
            _player.SetHorizontalPosition(
                _scenes[_activeSceneIndex].Bounds.Right - _player.Bounds.Width - SceneEntryInset);
        }

        if (_activeSceneIndex != sceneIndexBeforeUpdate)
        {
            SaveGame();
        }

        if (_scenes.SelectMany(currentScene => currentScene.Enemies).All(enemy => !enemy.IsAlive))
        {
            _cutsceneOverlay.Play("Encounter Clear", "The path opens once more", 1.8f);
            _combatEffects.AddParrySpark(new Vector2(_player.Bounds.Center.X, _player.Bounds.Center.Y));
            _gameState = GameState.Victory;
            SaveGame();
            _previousGameKeyboard = keyboardState;
            base.Update(gameTime);
            return;
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
            _mainMenu.Draw(
                _spriteBatch,
                GraphicsDevice.Viewport.Bounds,
                _saveSlots,
                _settings);
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
        _weapon.Draw(_spriteBatch, _player, _pixelTexture);
        _combatEffects.Draw(_spriteBatch, _pixelTexture);
        _spriteBatch.End();

        _spriteBatch.Begin();
        DrawPlayerHealth();
        DrawInteractionHint();
        _cutsceneOverlay.Draw(_spriteBatch, GraphicsDevice.Viewport.Bounds);
        _storyPlayer.Draw(_spriteBatch, GraphicsDevice.Viewport.Bounds);
        if (_gameState == GameState.Victory)
        {
            DrawVictory();
        }
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void RestartGame()
    {
        _activeSceneIndex = 0;
        _player.ResetPosition(_scenes[_activeSceneIndex].PlayerStart);
        _player.ResetHealth();
        _weapon.Reset();
        _combatEffects.Clear();
        foreach (var scene in _scenes)
        {
            foreach (var enemy in scene.Enemies)
            {
                enemy.Reset();
            }
        }
        _camera.Follow(_player, _scenes[_activeSceneIndex].Bounds, GraphicsDevice.Viewport);
    }

    private void StartNewGame(int slot)
    {
        _activeSaveSlot = slot;
        RestartGame();
        SaveGame();
        _gameState = GameState.Playing;
        _mainMenu.SetStatusMessage(string.Empty);
    }

    private void ContinueGame(int slot)
    {
        try
        {
            var save = _saveManager.LoadSlot(slot);
            if (save.SceneIndex >= _scenes.Length)
            {
                throw new InvalidDataException("存档中的场景编号无效。");
            }

            _activeSceneIndex = save.SceneIndex;
            _activeSaveSlot = slot;
            foreach (var scene in _scenes)
            {
                foreach (var enemy in scene.Enemies)
                {
                    enemy.Reset();
                }
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

            for (var sceneIndex = 0; sceneIndex < _scenes.Length; sceneIndex++)
            {
                var enemies = _scenes[sceneIndex].Enemies;
                for (var i = 0; i < enemies.Count; i++)
                {
                    var hasSceneData = sceneIndex < save.SceneEnemyHealths.Length &&
                        sceneIndex < save.SceneEnemyXs.Length &&
                        save.SceneEnemyHealths[sceneIndex] != null &&
                        save.SceneEnemyXs[sceneIndex] != null;
                    var health = hasSceneData && i < save.SceneEnemyHealths[sceneIndex].Length
                        ? save.SceneEnemyHealths[sceneIndex][i]
                        : sceneIndex == 1 && i < save.EnemyHealths.Length
                            ? save.EnemyHealths[i]
                            : sceneIndex == 1 && i == 0
                                ? save.EnemyHealth
                                : enemies[i].MaxHealth;
                    var position = hasSceneData && i < save.SceneEnemyXs[sceneIndex].Length
                        ? save.SceneEnemyXs[sceneIndex][i]
                        : sceneIndex == 1 && i < save.EnemyXs.Length
                            ? save.EnemyXs[i]
                            : sceneIndex == 1 && i == 0
                                ? save.EnemyX
                                : enemies[i].PositionX;
                    enemies[i].RestoreState(health, position, _scenes[sceneIndex].Bounds);
                }
            }
            _camera.Follow(_player, sceneBounds, GraphicsDevice.Viewport);
            _gameState = _scenes.SelectMany(currentScene => currentScene.Enemies).All(enemy => !enemy.IsAlive)
                ? GameState.Victory
                : GameState.Playing;
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
            var enemies = _scenes.Length > 1 ? _scenes[1].Enemies : System.Array.Empty<Enemy>();
            _saveManager.SaveSlot(_activeSaveSlot, new SaveData
            {
                SceneIndex = _activeSceneIndex,
                PlayerX = _player.Position.X,
                PlayerY = _player.Position.Y,
                PlayerHealth = _player.Health,
                EnemyHealth = enemies.Count > 0 ? enemies[0].Health : 0,
                EnemyX = enemies.Count > 0 ? enemies[0].PositionX : 0,
                EnemyHealths = enemies.Select(enemy => enemy.Health).ToArray(),
                EnemyXs = enemies.Select(enemy => enemy.PositionX).ToArray(),
                SceneEnemyHealths = _scenes.Select(currentScene =>
                    currentScene.Enemies.Select(enemy => enemy.Health).ToArray()).ToArray(),
                SceneEnemyXs = _scenes.Select(currentScene =>
                    currentScene.Enemies.Select(enemy => enemy.PositionX).ToArray()).ToArray()
            });
            _saveSlots = _saveManager.GetSlots();
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

    private void DeleteSave(int slot)
    {
        try
        {
            _saveManager.DeleteSlot(slot);
            _saveSlots = _saveManager.GetSlots();
            _mainMenu.CompleteSlotDeletion();
            _mainMenu.SetStatusMessage($"Slot {slot} deleted.");
        }
        catch (IOException exception)
        {
            _mainMenu.SetStatusMessage($"Delete failed: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            _mainMenu.SetStatusMessage($"Cannot delete save: {exception.Message}");
        }
    }

    private void ApplySettings()
    {
        var settings = _mainMenu.Settings.Copy();
        var resolution = MainMenu.GetResolution(settings.ResolutionIndex);
        _graphics.PreferredBackBufferWidth = resolution.X;
        _graphics.PreferredBackBufferHeight = resolution.Y;
        _graphics.IsFullScreen = settings.Fullscreen;
        _graphics.SynchronizeWithVerticalRetrace = settings.VSync;

        try
        {
            _graphics.ApplyChanges();
            BuildScenes(GraphicsDevice.Viewport);
            RestartGame();
            _settings = settings;
            _combatEffects.ReduceMotion = settings.ReduceMotion;
            _cutsceneOverlay.ReduceMotion = settings.ReduceMotion;
            _mainMenu.SetAppliedSettings(settings);
            _mainMenu.ResetInput();
        }
        catch (InvalidOperationException exception)
        {
            _mainMenu.SetStatusMessage($"Graphics settings could not be applied: {exception.Message}");
            return;
        }

        try
        {
            _saveManager.SaveSettings(settings);
            _mainMenu.SetStatusMessage("Settings applied and saved.");
        }
        catch (IOException exception)
        {
            _mainMenu.SetStatusMessage($"Settings applied, but could not be saved: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            _mainMenu.SetStatusMessage($"Settings applied, but cannot be saved: {exception.Message}");
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

        var postureBackground = new Rectangle(16, 40, 200, 10);
        var postureFill = new Rectangle(
            postureBackground.X,
            postureBackground.Y,
            (int)(postureBackground.Width * (_player.Posture / _player.MaxPosture)),
            postureBackground.Height);
        _spriteBatch.Draw(_pixelTexture, postureBackground, Color.DarkSlateGray);
        _spriteBatch.Draw(_pixelTexture, postureFill, Color.Gold);

        if (_weapon.IsAttacking && !string.IsNullOrEmpty(_weapon.ActiveAttackName))
        {
            var size = _menuFont.MeasureString(_weapon.ActiveAttackName);
            _spriteBatch.DrawString(
                _menuFont,
                _weapon.ActiveAttackName,
                new Vector2(
                    GraphicsDevice.Viewport.Width / 2f - size.X / 2f,
                    GraphicsDevice.Viewport.Height - size.Y - 64f),
                Color.Gold);
        }
    }

    private void DrawInteractionHint()
    {
        var npc = _scenes[_activeSceneIndex].Npc;
        if (npc != null && npc.IsPlayerNearby(_player))
        {
            DrawCenteredHint("E - TALK");
            return;
        }

        var canDeathblow = _scenes[_activeSceneIndex].Enemies.Any(enemy => enemy.CanDeathblow(_player));
        if (canDeathblow)
        {
            DrawCenteredHint("E - DEATHBLOW");
        }
    }

    private void DrawCenteredHint(string hint)
    {
        var size = _menuFont.MeasureString(hint);
        _spriteBatch.DrawString(
            _menuFont,
            hint,
            new Vector2(
                GraphicsDevice.Viewport.Width / 2f - size.X / 2f,
                GraphicsDevice.Viewport.Height - size.Y - 24f),
            Color.Gold);
    }

    private void DrawVictory()
    {
        var viewport = GraphicsDevice.Viewport.Bounds;
        var panel = new Rectangle(viewport.Width / 2 - 260, viewport.Height / 2 - 90, 520, 180);
        _spriteBatch.Draw(_pixelTexture, panel, new Color(12, 14, 20, 225));
        DrawCenteredText("ENEMY DEFEATED", panel.Top + 42, Color.Gold);
        DrawCenteredText("Press Enter or Esc to return to the menu", panel.Top + 100, Color.White);
    }

    private void DrawCenteredText(string text, float y, Color color)
    {
        var size = _menuFont.MeasureString(text);
        _spriteBatch.DrawString(
            _menuFont,
            text,
            new Vector2(GraphicsDevice.Viewport.Width / 2f - size.X / 2f, y),
            color);
    }
}
