using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace test.UI;

public enum MenuAction
{
    None,
    ContinueGame,
    StartGame,
    ApplyResolution,
    Exit
}

public sealed class MainMenu
{
    private enum Page
    {
        Main,
        Controls,
        Settings
    }

    private static readonly Point[] Resolutions =
    {
        new(1280, 720),
        new(1600, 900),
        new(1920, 1080)
    };

    private readonly Texture2D _background;
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;
    private KeyboardState _previousKeyboard;
    private MouseState _previousMouse;
    private int _selectedIndex;
    private int _resolutionIndex;
    private Page _page;
    public string StatusMessage { get; private set; } = string.Empty;
    public Point SelectedResolution => Resolutions[_resolutionIndex];

    public MainMenu(Texture2D background, Texture2D pixel, SpriteFont font)
    {
        _background = background;
        _pixel = pixel;
        _font = font;
    }

    public void ResetInput()
    {
        _previousKeyboard = Keyboard.GetState();
        _previousMouse = Mouse.GetState();
    }

    public void SetStatusMessage(string message)
    {
        StatusMessage = message ?? string.Empty;
    }

    public MenuAction Update(Rectangle viewport, bool hasSave)
    {
        var keyboard = Keyboard.GetState();
        var mouse = Mouse.GetState();
        var buttons = GetButtons(viewport);
        var itemCount = _page == Page.Main ? 5 : _page == Page.Settings ? 3 : 1;

        if (Pressed(keyboard, Keys.Escape))
        {
            if (_page != Page.Main)
            {
                var previousPage = _page;
                _page = Page.Main;
                _selectedIndex = previousPage == Page.Controls ? 2 : 3;
            }
            else
            {
                return MenuAction.Exit;
            }
        }

        if (Pressed(keyboard, Keys.Up) || Pressed(keyboard, Keys.W))
        {
            _selectedIndex = (_selectedIndex + itemCount - 1) % itemCount;
        }
        if (Pressed(keyboard, Keys.Down) || Pressed(keyboard, Keys.S))
        {
            _selectedIndex = (_selectedIndex + 1) % itemCount;
        }
        if (_page == Page.Settings && _selectedIndex == 0)
        {
            if (Pressed(keyboard, Keys.Left) || Pressed(keyboard, Keys.A))
            {
                ChangeResolution(-1);
            }
            if (Pressed(keyboard, Keys.Right) || Pressed(keyboard, Keys.D))
            {
                ChangeResolution(1);
            }
        }

        for (var i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].Contains(mouse.Position))
            {
                _selectedIndex = i;
                if (mouse.LeftButton == ButtonState.Pressed &&
                    _previousMouse.LeftButton == ButtonState.Released)
                {
                    if (_page == Page.Settings && i == 0)
                    {
                        ChangeResolution(1);
                        continue;
                    }

                    var action = SelectItem(i);
                    if (action != MenuAction.None)
                    {
                        SaveInput(keyboard, mouse);
                        return action;
                    }
                }
            }
        }

        if (Pressed(keyboard, Keys.Enter) || Pressed(keyboard, Keys.Space))
        {
            var action = SelectItem(_selectedIndex);
            if (action != MenuAction.None)
            {
                SaveInput(keyboard, mouse);
                return action;
            }
        }

        SaveInput(keyboard, mouse);
        return MenuAction.None;
    }

    public void Draw(SpriteBatch spriteBatch, Rectangle viewport, bool hasSave)
    {
        spriteBatch.Draw(_background, viewport, Color.White);
        spriteBatch.Draw(_pixel, viewport, new Color(0, 0, 0, 145));

        var buttons = GetButtons(viewport);
        var labels = _page switch
        {
            Page.Main => new[]
            {
                hasSave ? "CONTINUE" : "CONTINUE (NO SAVE)",
                "NEW GAME",
                "CONTROLS",
                "SETTINGS",
                "EXIT"
            },
            Page.Settings => new[]
            {
                $"{SelectedResolution.X} x {SelectedResolution.Y}",
                "APPLY",
                "BACK"
            },
            _ => new[] { "BACK" }
        };

        if (_page == Page.Controls)
        {
            DrawCentered(spriteBatch, "CONTROLS", viewport.Center.X, viewport.Height / 2 - 110, Color.White);
            DrawCentered(spriteBatch, "Move: A / D or arrow keys", viewport.Center.X, viewport.Height / 2 - 55, Color.White);
            DrawCentered(spriteBatch, "Run: hold SHIFT", viewport.Center.X, viewport.Height / 2 - 15, Color.White);
            DrawCentered(spriteBatch, "Jump: SPACE, W or UP", viewport.Center.X, viewport.Height / 2 + 25, Color.White);
            DrawCentered(spriteBatch, "Combo: J for slash, thrust, then dash", viewport.Center.X, viewport.Height / 2 + 65, Color.White);
            DrawCentered(spriteBatch, "Wait 1 second to reset the combo", viewport.Center.X, viewport.Height / 2 + 105, Color.LightGray);
        }
        else if (_page == Page.Settings)
        {
            DrawCentered(spriteBatch, "SETTINGS", viewport.Center.X, viewport.Height / 2 - 120, Color.White);
            DrawCentered(spriteBatch, "RESOLUTION - use LEFT/RIGHT to choose", viewport.Center.X, viewport.Height / 2 - 78, Color.LightGray);
        }

        for (var i = 0; i < buttons.Length; i++)
        {
            var isSelected = i == _selectedIndex;
            spriteBatch.Draw(
                _pixel,
                buttons[i],
                isSelected ? new Color(27, 69, 75, 235) : new Color(12, 17, 22, 220));
            DrawCentered(
                spriteBatch,
                labels[i],
                buttons[i].Center.X,
                buttons[i].Center.Y,
                isSelected ? Color.Gold : Color.White);
        }

        var hint = _page == Page.Main
            ? "Use UP/DOWN and ENTER, or click a button"
            : "Press ESC to return";
        DrawCentered(spriteBatch, hint, viewport.Center.X, viewport.Bottom - 34, Color.LightGray);
        if (!string.IsNullOrWhiteSpace(StatusMessage))
        {
            DrawCentered(spriteBatch, StatusMessage, viewport.Center.X, viewport.Bottom - 72, Color.OrangeRed);
        }
    }

    private MenuAction SelectItem(int index)
    {
        if (_page == Page.Controls)
        {
            _page = Page.Main;
            _selectedIndex = 2;
            return MenuAction.None;
        }
        if (_page == Page.Settings)
        {
            switch (index)
            {
                case 0:
                    ChangeResolution(1);
                    return MenuAction.None;
                case 1:
                    return MenuAction.ApplyResolution;
                case 2:
                    _page = Page.Main;
                    _selectedIndex = 3;
                    return MenuAction.None;
                default:
                    return MenuAction.None;
            }
        }

        switch (index)
        {
            case 0:
                return MenuAction.ContinueGame;
            case 1:
                return MenuAction.StartGame;
            case 2:
                _page = Page.Controls;
                _selectedIndex = 0;
                return MenuAction.None;
            case 3:
                _page = Page.Settings;
                _selectedIndex = 0;
                return MenuAction.None;
            case 4:
                return MenuAction.Exit;
            default:
                return MenuAction.None;
        }
    }

    private Rectangle[] GetButtons(Rectangle viewport)
    {
        var buttonWidth = System.Math.Min(300, viewport.Width - 48);
        var buttonHeight = 54;
        var gap = 14;
        var count = _page == Page.Main ? 5 : _page == Page.Settings ? 3 : 1;
        var totalHeight = count * buttonHeight + (count - 1) * gap;
        var startY = _page == Page.Main
            ? viewport.Height * 58 / 100 - totalHeight / 2
            : viewport.Height * 3 / 4 - buttonHeight / 2;
        var buttons = new Rectangle[count];

        for (var i = 0; i < count; i++)
        {
            buttons[i] = new Rectangle(
                viewport.Center.X - buttonWidth / 2,
                startY + i * (buttonHeight + gap),
                buttonWidth,
                buttonHeight);
        }

        return buttons;
    }

    private void DrawCentered(SpriteBatch spriteBatch, string text, int centerX, int centerY, Color color)
    {
        var size = _font.MeasureString(text);
        spriteBatch.DrawString(_font, text, new Vector2(centerX - size.X / 2f, centerY - size.Y / 2f), color);
    }

    private bool Pressed(KeyboardState current, Keys key) =>
        current.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);

    private void ChangeResolution(int direction)
    {
        _resolutionIndex = (_resolutionIndex + direction + Resolutions.Length) % Resolutions.Length;
    }

    private void SaveInput(KeyboardState keyboard, MouseState mouse)
    {
        _previousKeyboard = keyboard;
        _previousMouse = mouse;
    }
}
