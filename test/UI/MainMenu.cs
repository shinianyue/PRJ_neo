using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using test.Persistence;

namespace test.UI;

public enum MenuAction
{
    None,
    LoadSlot,
    NewGameSlot,
    DeleteSlot,
    ApplySettings,
    Exit
}

public sealed class MainMenu
{
    private sealed class RepeatInput
    {
        private int _direction;
        private float _elapsed;
        private bool _hasRepeated;

        public int Update(int direction, float elapsed)
        {
            if (direction == 0)
            {
                _direction = 0;
                _elapsed = 0f;
                _hasRepeated = false;
                return 0;
            }

            if (direction != _direction)
            {
                _direction = direction;
                _elapsed = 0f;
                _hasRepeated = false;
                return direction;
            }

            _elapsed += elapsed;
            const float initialDelay = 0.24f;
            const float repeatInterval = 0.085f;
            if (!_hasRepeated)
            {
                if (_elapsed < initialDelay)
                {
                    return 0;
                }

                _elapsed -= initialDelay;
                _hasRepeated = true;
                return direction;
            }

            if (_elapsed < repeatInterval)
            {
                return 0;
            }

            _elapsed -= repeatInterval;
            return direction;
        }

        public void Reset()
        {
            _direction = 0;
            _elapsed = 0f;
            _hasRepeated = false;
        }
    }

    private enum Page
    {
        Main,
        Slots,
        Settings,
        Controls,
        Confirm
    }

    private enum SlotMode
    {
        Load,
        NewGame
    }

    private enum ConfirmAction
    {
        Overwrite,
        Delete
    }

    private static readonly Point[] Resolutions =
    {
        new(1280, 720),
        new(1600, 900),
        new(1920, 1080)
    };

    private readonly Texture2D _background;
    private readonly Texture2D _pixel;
    private readonly Texture2D _hero;
    private readonly SpriteFont _font;
    private readonly GameSettings _appliedSettings;
    private string[] _sceneNames = { "ASHEN GATE", "BROKEN SHRINE", "SUNKEN ALTAR" };
    private GameSettings _workingSettings;
    private KeyboardState _previousKeyboard;
    private MouseState _previousMouse;
    private int _selectedIndex;
    private int _selectedSlot = 1;
    private Page _page;
    private SlotMode _slotMode;
    private ConfirmAction _confirmAction;
    private float _time;
    private float _pageProgress = 1f;
    private float _statusTimer;
    private readonly RepeatInput _verticalRepeat = new();
    private readonly RepeatInput _horizontalRepeat = new();

    public string StatusMessage { get; private set; } = string.Empty;
    public int SelectedSlot => _selectedSlot;
    public GameSettings Settings => _workingSettings;
    public static Point GetResolution(int index) => Resolutions[index];

    public MainMenu(
        Texture2D background,
        Texture2D pixel,
        Texture2D hero,
        SpriteFont font,
        GameSettings settings)
    {
        _background = background;
        _pixel = pixel;
        _hero = hero;
        _font = font;
        _appliedSettings = settings.Copy();
        _workingSettings = settings.Copy();
    }

    public void ResetInput()
    {
        _previousKeyboard = Keyboard.GetState();
        _previousMouse = Mouse.GetState();
        _verticalRepeat.Reset();
        _horizontalRepeat.Reset();
    }

    public void SetStatusMessage(string message)
    {
        StatusMessage = message ?? string.Empty;
        _statusTimer = 4f;
    }

    public void SetSceneNames(IEnumerable<string> sceneNames)
    {
        _sceneNames = sceneNames?.ToArray() ?? Array.Empty<string>();
    }

    public void SetAppliedSettings(GameSettings settings)
    {
        CopySettings(settings, _appliedSettings);
        _workingSettings = settings.Copy();
    }

    public void CompleteSlotDeletion()
    {
        OpenPage(Page.Slots);
        _selectedIndex = _selectedSlot - 1;
    }

    public MenuAction Update(GameTime gameTime, Rectangle viewport, SaveSlotInfo[] slots)
    {
        var elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_appliedSettings.ReduceMotion)
        {
            _pageProgress = 1f;
        }
        else
        {
            _time += elapsed;
            _pageProgress = Math.Min(1f, _pageProgress + elapsed * 8f);
        }
        _statusTimer = Math.Max(0f, _statusTimer - elapsed);

        var keyboard = Keyboard.GetState();
        var mouse = Mouse.GetState();
        var buttons = GetButtons(viewport);
        var itemCount = buttons.Length;

        if (Pressed(keyboard, Keys.Escape))
        {
            if (_page == Page.Main)
            {
                SaveInput(keyboard, mouse);
                return MenuAction.Exit;
            }

            GoBack();
            SaveInput(keyboard, mouse);
            return MenuAction.None;
        }

        var verticalDirection =
            (keyboard.IsKeyDown(Keys.Down) || keyboard.IsKeyDown(Keys.S) ? 1 : 0) -
            (keyboard.IsKeyDown(Keys.Up) || keyboard.IsKeyDown(Keys.W) ? 1 : 0);
        var navigation = _verticalRepeat.Update(verticalDirection, elapsed);
        if (navigation < 0)
        {
            _selectedIndex = (_selectedIndex + itemCount - 1) % itemCount;
        }
        else if (navigation > 0)
        {
            _selectedIndex = (_selectedIndex + 1) % itemCount;
        }

        if (_page == Page.Settings)
        {
            var horizontalDirection =
                (keyboard.IsKeyDown(Keys.Right) || keyboard.IsKeyDown(Keys.D) ? 1 : 0) -
                (keyboard.IsKeyDown(Keys.Left) || keyboard.IsKeyDown(Keys.A) ? 1 : 0);
            var settingDirection = _horizontalRepeat.Update(horizontalDirection, elapsed);
            if (settingDirection < 0)
            {
                AdjustSetting(-1);
            }
            else if (settingDirection > 0)
            {
                AdjustSetting(1);
            }
        }
        else
        {
            _horizontalRepeat.Reset();
        }

        if (_page == Page.Slots && Pressed(keyboard, Keys.Delete) && _selectedIndex < SaveManager.SlotCount)
        {
            var selected = slots[_selectedIndex];
            if (selected.HasSave || selected.IsCorrupt)
            {
                BeginConfirmation(ConfirmAction.Delete, selected.Slot);
                SaveInput(keyboard, mouse);
                return MenuAction.None;
            }
            else if (selected.IsCorrupt)
            {
                SetStatusMessage($"Slot {selected.Slot}: {selected.Error}");
            }
        }

        for (var i = 0; i < buttons.Length; i++)
        {
            if (!buttons[i].Contains(mouse.Position))
            {
                continue;
            }

            var mousePressed = mouse.LeftButton == ButtonState.Pressed &&
                _previousMouse.LeftButton == ButtonState.Released;
            if (mouse.Position != _previousMouse.Position || mousePressed)
            {
                _selectedIndex = i;
            }

            if (mousePressed)
            {
                if (_page == Page.Settings && i <= 3)
                {
                    AdjustSetting(1);
                    continue;
                }

                var action = SelectItem(i, slots);
                if (action != MenuAction.None)
                {
                    SaveInput(keyboard, mouse);
                    return action;
                }
            }
        }

        if (Pressed(keyboard, Keys.Enter) || Pressed(keyboard, Keys.Space))
        {
            var action = SelectItem(_selectedIndex, slots);
            if (action != MenuAction.None)
            {
                SaveInput(keyboard, mouse);
                return action;
            }
        }

        SaveInput(keyboard, mouse);
        return MenuAction.None;
    }

    public void Draw(
        SpriteBatch spriteBatch,
        Rectangle viewport,
        SaveSlotInfo[] slots,
        GameSettings settings)
    {
        spriteBatch.Draw(_background, viewport, new Color(190, 195, 205));
        spriteBatch.Draw(_pixel, viewport, new Color(4, 7, 12, 166));
        DrawAtmosphere(spriteBatch, viewport, settings.ReduceMotion);

        var panelWidth = Math.Min(510, viewport.Width - 40);
        var panelHeight = Math.Min(590, viewport.Height - 48);
        var panel = new Rectangle(
            viewport.Right - panelWidth - 28,
            viewport.Center.Y - panelHeight / 2,
            panelWidth,
            panelHeight);
        var slide = (int)((1f - _pageProgress) * 16f);
        panel.X += slide;

        spriteBatch.Draw(_pixel, panel, new Color(7, 11, 17, 226));
        DrawBorder(spriteBatch, panel, new Color(191, 153, 82, 170));
        DrawBrand(spriteBatch, viewport, panel);
        DrawPage(spriteBatch, panel, slots, settings);

        if (_pageProgress < 1f)
        {
            var alpha = (byte)((1f - _pageProgress) * 100f);
            spriteBatch.Draw(_pixel, viewport, new Color(0, 0, 0, (int)alpha));
        }

        if (!string.IsNullOrWhiteSpace(StatusMessage) && _statusTimer > 0f)
        {
            DrawCentered(
                spriteBatch,
                StatusMessage,
                viewport.Center.X,
                viewport.Bottom - 30,
                Color.Orange);
        }
    }

    private void DrawBrand(SpriteBatch spriteBatch, Rectangle viewport, Rectangle panel)
    {
        var brandCenter = (viewport.Left + panel.Left) / 2;
        DrawCentered(spriteBatch, "PROJECT SNY", brandCenter, viewport.Height / 4, Color.Gold, 1.25f);
        DrawCentered(spriteBatch, "THE LAST EMBER", brandCenter, viewport.Height / 4 + 38, Color.White);

        var heroSource = new Rectangle(0, 0, _hero.Width, Math.Min(58, _hero.Height));
        var heroScale = Math.Min(3.2f, viewport.Height / 250f);
        var heroPosition = new Vector2(brandCenter, viewport.Height * 0.57f);
        spriteBatch.Draw(
            _hero,
            heroPosition,
            heroSource,
            Color.White * (0.78f + (float)Math.Sin(_time * 1.5f) * 0.08f),
            0f,
            new Vector2(heroSource.Width / 2f, heroSource.Height / 2f),
            heroScale,
            SpriteEffects.None,
            0f);

        DrawCentered(spriteBatch, "A SHINOBI'S JOURNEY", brandCenter, viewport.Height * 0.77f, Color.LightGray);
        DrawCentered(spriteBatch, "WALK THE ASHEN ROAD", brandCenter, viewport.Height * 0.77f + 32, Color.DarkGoldenrod);
        var lineWidth = Math.Min(300, Math.Max(80, panel.Left - viewport.Left - 70));
        spriteBatch.Draw(
            _pixel,
            new Rectangle(brandCenter - lineWidth / 2, (int)(viewport.Height * 0.84f), lineWidth, 1),
            Color.Goldenrod * 0.55f);
        DrawCentered(spriteBatch, "ARROWS / WASD   SELECT", brandCenter, viewport.Height * 0.88f, Color.LightGray * 0.8f);
        DrawCentered(spriteBatch, "ENTER   CONFIRM     ESC   BACK", brandCenter, viewport.Height * 0.92f, Color.LightGray * 0.8f);
    }

    private void DrawPage(SpriteBatch spriteBatch, Rectangle panel, SaveSlotInfo[] slots, GameSettings settings)
    {
        var title = _page switch
        {
            Page.Main => "MAIN MENU",
            Page.Slots => _slotMode == SlotMode.Load ? "LOAD GAME" : "NEW GAME",
            Page.Settings => "SETTINGS",
            Page.Controls => "HOW TO PLAY",
            _ => _confirmAction == ConfirmAction.Delete ? "DELETE SAVE?" : "OVERWRITE SAVE?"
        };
        DrawCentered(spriteBatch, title, panel.Center.X, panel.Top + 48, Color.Gold, 1.05f);
        spriteBatch.Draw(_pixel, new Rectangle(panel.Left + 34, panel.Top + 78, panel.Width - 68, 1), Color.Goldenrod * 0.65f);

        var buttons = GetButtons(panel);
        if (_page == Page.Main)
        {
            var labels = new[] { "CONTINUE", "NEW GAME", "SETTINGS", "HOW TO PLAY", "EXIT" };
            DrawButtons(spriteBatch, buttons, labels);
        }
        else if (_page == Page.Slots)
        {
            DrawSlots(spriteBatch, buttons, slots);
        }
        else if (_page == Page.Settings)
        {
            DrawSettings(spriteBatch, buttons, _workingSettings);
        }
        else if (_page == Page.Controls)
        {
            DrawControls(spriteBatch, panel, buttons);
        }
        else
        {
            DrawConfirmation(spriteBatch, panel, buttons);
        }

        var hint = _page == Page.Main ? "UP / DOWN   SELECT      ENTER   CONFIRM" :
            _page == Page.Slots ? "ENTER   SELECT      DEL   DELETE      ESC   BACK" :
            _page == Page.Settings ? "LEFT / RIGHT   CHANGE      ENTER   SELECT      ESC   BACK" :
            "ENTER   SELECT      ESC   BACK";
        DrawCentered(spriteBatch, hint, panel.Center.X, panel.Bottom - 29, Color.LightGray * 0.85f, 0.72f);
    }

    private void DrawButtons(SpriteBatch spriteBatch, Rectangle[] buttons, string[] labels)
    {
        for (var i = 0; i < buttons.Length; i++)
        {
            DrawButton(spriteBatch, buttons[i], labels[i], i == _selectedIndex);
        }
    }

    private void DrawSlots(SpriteBatch spriteBatch, Rectangle[] buttons, SaveSlotInfo[] slots)
    {
        for (var i = 0; i < SaveManager.SlotCount; i++)
        {
            var slot = slots[i];
            var bounds = buttons[i];
            var selected = i == _selectedIndex;
            spriteBatch.Draw(_pixel, bounds, selected ? new Color(47, 43, 31, 245) : new Color(17, 23, 29, 235));
            DrawBorder(spriteBatch, bounds, selected ? Color.Gold : new Color(126, 119, 99, 130));
            DrawText(spriteBatch, $"SLOT {slot.Slot}", bounds.Left + 20, bounds.Top + 10, selected ? Color.Gold : Color.White);

            if (slot.HasSave)
            {
                var data = slot.Data;
                var scene = data.SceneIndex >= 0 && data.SceneIndex < _sceneNames.Length
                    ? _sceneNames[data.SceneIndex]
                    : "UNKNOWN REGION";
                DrawText(spriteBatch, $"{scene}   |   HP {data.PlayerHealth}/100", bounds.Left + 20, bounds.Top + 40, Color.White);
                var saved = data.SavedAtUtc == default ? "LEGACY SAVE" : data.SavedAtUtc.ToLocalTime().ToString("yyyy-MM-dd  HH:mm");
                DrawText(spriteBatch, saved, bounds.Left + 20, bounds.Top + 66, Color.LightGray * 0.8f, 0.72f);
                DrawText(spriteBatch, "READY", bounds.Right - 98, bounds.Top + 12, Color.LightGreen, 0.72f);
            }
            else if (slot.IsCorrupt)
            {
                DrawText(spriteBatch, "SAVE DATA ERROR", bounds.Left + 20, bounds.Top + 43, Color.OrangeRed, 0.78f);
            }
            else
            {
                DrawText(spriteBatch, "EMPTY SLOT", bounds.Left + 20, bounds.Top + 43, Color.LightGray * 0.75f);
            }
        }

        DrawButton(spriteBatch, buttons[3], "BACK", _selectedIndex == 3);
    }

    private void DrawSettings(SpriteBatch spriteBatch, Rectangle[] buttons, GameSettings settings)
    {
        var values = new[]
        {
            $"RESOLUTION       {Resolutions[settings.ResolutionIndex].X} x {Resolutions[settings.ResolutionIndex].Y}",
            $"FULLSCREEN       {(settings.Fullscreen ? "ON" : "OFF")}",
            $"V-SYNC           {(settings.VSync ? "ON" : "OFF")}",
            $"REDUCE MOTION    {(settings.ReduceMotion ? "ON" : "OFF")}",
            "APPLY SETTINGS",
            "BACK"
        };

        for (var i = 0; i < buttons.Length; i++)
        {
            DrawButton(spriteBatch, buttons[i], values[i], i == _selectedIndex);
        }
    }

    private void DrawControls(SpriteBatch spriteBatch, Rectangle panel, Rectangle[] buttons)
    {
        var rows = new[]
        {
            "MOVE                 A / D  OR  ARROWS",
            "RUN                  HOLD  SHIFT",
            "JUMP                 SPACE / W / UP",
            "ATTACK               J  (FIVE-HIT COMBO)",
            "SPECIAL              U  (QUICKDRAW / MOONPIERCER)",
            "PARRY                TAP  CTRL / K",
            "TALK / DEATHBLOW     E",
            "PAUSE / MENU         ESC"
        };

        for (var i = 0; i < rows.Length; i++)
        {
            DrawCentered(spriteBatch, rows[i], panel.Center.X, panel.Top + 120 + i * 44, Color.White, 0.78f);
        }

        DrawButton(spriteBatch, buttons[0], "BACK", true);
    }

    private void DrawConfirmation(SpriteBatch spriteBatch, Rectangle panel, Rectangle[] buttons)
    {
        var prompt = _confirmAction == ConfirmAction.Delete
            ? $"DELETE SLOT {_selectedSlot}? THIS CANNOT BE UNDONE."
            : $"OVERWRITE SLOT {_selectedSlot}?";
        DrawCentered(spriteBatch, prompt, panel.Center.X, panel.Center.Y - 36, Color.White, 0.78f);
        DrawButton(spriteBatch, buttons[0], "YES", _selectedIndex == 0);
        DrawButton(spriteBatch, buttons[1], "NO", _selectedIndex == 1);
    }

    private void DrawButton(SpriteBatch spriteBatch, Rectangle bounds, string label, bool selected)
    {
        spriteBatch.Draw(
            _pixel,
            bounds,
            selected ? new Color(52, 48, 35, 248) : new Color(16, 21, 27, 225));
        DrawBorder(spriteBatch, bounds, selected ? Color.Gold : new Color(131, 125, 112, 150));
        DrawCentered(
            spriteBatch,
            label,
            bounds.Center.X,
            bounds.Center.Y,
            selected ? Color.Gold : Color.White,
            0.82f);
        if (selected)
        {
            spriteBatch.Draw(
                _pixel,
                new Rectangle(bounds.Left + 10, bounds.Top + 10, 3, bounds.Height - 20),
                Color.Gold);
        }
    }

    private void DrawAtmosphere(SpriteBatch spriteBatch, Rectangle viewport, bool reduceMotion)
    {
        var overlay = new Rectangle(0, 0, viewport.Width, viewport.Height);
        spriteBatch.Draw(_pixel, overlay, new Color(8, 12, 20, 42));

        for (var i = 0; !reduceMotion && i < 28; i++)
        {
            var seed = i * 73.13f;
            var x = (int)((seed * 37f + _time * (8f + i % 5)) % viewport.Width);
            var y = (int)((seed * 19f - _time * (5f + i % 4) + viewport.Height * 4f) % viewport.Height);
            var alpha = 0.2f + (float)(Math.Sin(_time * 1.7f + seed) + 1f) * 0.25f;
            var size = i % 7 == 0 ? 3 : 2;
            spriteBatch.Draw(_pixel, new Rectangle(x, y, size, size), Color.Gold * alpha);
        }

        if (!reduceMotion)
        {
            var sweep = (int)((_time * 34f) % (viewport.Width + 360)) - 180;
            spriteBatch.Draw(
                _pixel,
                new Rectangle(sweep, 0, 1, viewport.Height),
                Color.White * (0.035f * (1f - _pageProgress * 0.2f)));
        }
    }

    private Rectangle[] GetButtons(Rectangle bounds)
    {
        if (_page == Page.Main)
        {
            const int buttonHeight = 58;
            const int gap = 12;
            var width = Math.Min(390, bounds.Width - 52);
            var startY = bounds.Center.Y - (5 * buttonHeight + 4 * gap) / 2 + 20;
            return MakeButtons(bounds.Center.X, width, buttonHeight, gap, startY, 5);
        }

        if (_page == Page.Slots)
        {
            const int cardHeight = 94;
            const int gap = 11;
            var width = bounds.Width - 56;
            var totalHeight = 3 * cardHeight + 2 * gap + 22 + 48;
            var startY = bounds.Center.Y - totalHeight / 2;
            var cards = MakeButtons(bounds.Center.X, width, cardHeight, gap, startY, 3);
            var result = new Rectangle[4];
            Array.Copy(cards, result, cards.Length);
            result[3] = new Rectangle(bounds.Center.X - 110, startY + totalHeight - 48, 220, 42);
            return result;
        }

        if (_page == Page.Settings)
        {
            const int buttonHeight = 54;
            const int gap = 9;
            var startY = bounds.Center.Y - (6 * buttonHeight + 5 * gap) / 2 + 20;
            return MakeButtons(bounds.Center.X, bounds.Width - 56, buttonHeight, gap, startY, 6);
        }

        if (_page == Page.Controls)
        {
            return new[]
            {
                new Rectangle(bounds.Center.X - 110, bounds.Bottom - 78, 220, 44)
            };
        }

        return MakeButtons(bounds.Center.X, 210, 48, 14, bounds.Center.Y + 12, 2);
    }

    private static Rectangle[] MakeButtons(int centerX, int width, int height, int gap, int startY, int count)
    {
        var buttons = new Rectangle[count];
        for (var i = 0; i < count; i++)
        {
            buttons[i] = new Rectangle(centerX - width / 2, startY + i * (height + gap), width, height);
        }

        return buttons;
    }

    private MenuAction SelectItem(int index, SaveSlotInfo[] slots)
    {
        if (_page == Page.Main)
        {
            switch (index)
            {
                case 0:
                    OpenSlots(SlotMode.Load);
                    return MenuAction.None;
                case 1:
                    OpenSlots(SlotMode.NewGame);
                    return MenuAction.None;
                case 2:
                    _workingSettings = _appliedSettings.Copy();
                    OpenPage(Page.Settings);
                    return MenuAction.None;
                case 3:
                    OpenPage(Page.Controls);
                    return MenuAction.None;
                case 4:
                    return MenuAction.Exit;
            }
        }

        if (_page == Page.Slots)
        {
            if (index == SaveManager.SlotCount)
            {
                GoBack();
                return MenuAction.None;
            }

            var slot = slots[index];
            _selectedSlot = slot.Slot;
            if (slot.IsCorrupt)
            {
                if (_slotMode == SlotMode.NewGame)
                {
                    BeginConfirmation(ConfirmAction.Overwrite, slot.Slot);
                    return MenuAction.None;
                }

                SetStatusMessage($"Slot {slot.Slot}: {slot.Error}");
                return MenuAction.None;
            }

            if (_slotMode == SlotMode.Load)
            {
                return slot.HasSave ? MenuAction.LoadSlot : MenuAction.None;
            }

            if (slot.HasSave)
            {
                BeginConfirmation(ConfirmAction.Overwrite, slot.Slot);
                return MenuAction.None;
            }

            return MenuAction.NewGameSlot;
        }

        if (_page == Page.Settings)
        {
            if (index <= 3)
            {
                AdjustSetting(1);
                return MenuAction.None;
            }
            if (index == 4)
            {
                return MenuAction.ApplySettings;
            }

            _workingSettings = _appliedSettings.Copy();
            GoBack();
            return MenuAction.None;
        }

        if (_page == Page.Controls)
        {
            GoBack();
            return MenuAction.None;
        }

        if (index == 1)
        {
            GoBack();
            return MenuAction.None;
        }

        return _confirmAction == ConfirmAction.Delete
            ? MenuAction.DeleteSlot
            : MenuAction.NewGameSlot;
    }

    private void AdjustSetting(int direction)
    {
        switch (_selectedIndex)
        {
            case 0:
                _workingSettings.ResolutionIndex =
                    (_workingSettings.ResolutionIndex + direction + Resolutions.Length) % Resolutions.Length;
                break;
            case 1:
                _workingSettings.Fullscreen = !_workingSettings.Fullscreen;
                break;
            case 2:
                _workingSettings.VSync = !_workingSettings.VSync;
                break;
            case 3:
                _workingSettings.ReduceMotion = !_workingSettings.ReduceMotion;
                break;
        }
    }

    private void OpenSlots(SlotMode mode)
    {
        _slotMode = mode;
        _selectedIndex = 0;
        OpenPage(Page.Slots);
    }

    private void BeginConfirmation(ConfirmAction action, int slot)
    {
        _confirmAction = action;
        _selectedSlot = slot;
        _selectedIndex = 0;
        OpenPage(Page.Confirm);
    }

    private void GoBack()
    {
        if (_page == Page.Confirm)
        {
            OpenPage(Page.Slots);
            _selectedIndex = _selectedSlot - 1;
        }
        else if (_page == Page.Slots)
        {
            OpenPage(Page.Main);
        }
        else
        {
            OpenPage(Page.Main);
        }
    }

    private void OpenPage(Page page)
    {
        _page = page;
        _selectedIndex = 0;
        _pageProgress = 0f;
        _verticalRepeat.Reset();
        _horizontalRepeat.Reset();
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds, Color color)
    {
        spriteBatch.Draw(_pixel, new Rectangle(bounds.Left, bounds.Top, bounds.Width, 1), color);
        spriteBatch.Draw(_pixel, new Rectangle(bounds.Left, bounds.Bottom - 1, bounds.Width, 1), color);
        spriteBatch.Draw(_pixel, new Rectangle(bounds.Left, bounds.Top, 1, bounds.Height), color);
        spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - 1, bounds.Top, 1, bounds.Height), color);
    }

    private void DrawText(SpriteBatch spriteBatch, string text, float x, float y, Color color, float scale = 1f)
    {
        spriteBatch.DrawString(_font, text, new Vector2(x, y), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private void DrawCentered(
        SpriteBatch spriteBatch,
        string text,
        float centerX,
        float centerY,
        Color color,
        float scale = 0.85f)
    {
        var size = _font.MeasureString(text) * scale;
        DrawText(spriteBatch, text, centerX - size.X / 2f, centerY - size.Y / 2f, color, scale);
    }

    private bool Pressed(KeyboardState current, Keys key) =>
        current.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);

    private void SaveInput(KeyboardState keyboard, MouseState mouse)
    {
        _previousKeyboard = keyboard;
        _previousMouse = mouse;
    }

    private static void CopySettings(GameSettings source, GameSettings destination)
    {
        destination.ResolutionIndex = source.ResolutionIndex;
        destination.Fullscreen = source.Fullscreen;
        destination.VSync = source.VSync;
        destination.ReduceMotion = source.ReduceMotion;
    }
}
