using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace test.Story;

public sealed class StoryPlayer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly Dictionary<string, StorySequence> _sequences;
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;
    private KeyboardState _previousKeyboard;
    private StorySequence _activeSequence;
    private int _lineIndex;

    public bool IsPlaying => _activeSequence != null;

    public StoryPlayer(string storyDirectory, Texture2D pixel, SpriteFont font)
    {
        _pixel = pixel ?? throw new ArgumentNullException(nameof(pixel));
        _font = font ?? throw new ArgumentNullException(nameof(font));
        _sequences = LoadSequences(storyDirectory);
    }

    public void Start(string sequenceId)
    {
        if (!_sequences.TryGetValue(sequenceId, out var sequence))
        {
            throw new KeyNotFoundException($"找不到剧情序列“{sequenceId}”。");
        }

        _activeSequence = sequence;
        _lineIndex = 0;
        _previousKeyboard = Keyboard.GetState();
    }

    public bool Update()
    {
        if (!IsPlaying)
        {
            return true;
        }

        var keyboard = Keyboard.GetState();
        var skipPressed = keyboard.IsKeyDown(Keys.Escape) && _previousKeyboard.IsKeyUp(Keys.Escape);
        var advancePressed =
            (keyboard.IsKeyDown(Keys.Space) && _previousKeyboard.IsKeyUp(Keys.Space)) ||
            (keyboard.IsKeyDown(Keys.Enter) && _previousKeyboard.IsKeyUp(Keys.Enter));
        _previousKeyboard = keyboard;

        if (skipPressed)
        {
            _activeSequence = null;
            _lineIndex = 0;
            return true;
        }

        if (advancePressed)
        {
            _lineIndex++;
            if (_lineIndex >= _activeSequence.Lines.Count)
            {
                _activeSequence = null;
                _lineIndex = 0;
                return true;
            }
        }

        if (_lineIndex < 0)
        {
            _lineIndex = 0;
        }

        return false;
    }

    public void Draw(SpriteBatch spriteBatch, Rectangle viewport)
    {
        if (!IsPlaying)
        {
            return;
        }

        var panel = new Rectangle(
            viewport.Width / 12,
            viewport.Height * 2 / 3,
            viewport.Width * 5 / 6,
            viewport.Height / 4);
        spriteBatch.Draw(_pixel, panel, new Color(8, 10, 16, 235));

        var line = _activeSequence.Lines[_lineIndex];
        var textX = panel.X + 28;
        var textY = panel.Y + 22;
        if (!string.IsNullOrWhiteSpace(line.Speaker))
        {
            var speaker = StripUnsupportedCharacters(line.Speaker);
            if (!string.IsNullOrEmpty(speaker))
            {
                spriteBatch.DrawString(_font, speaker, new Vector2(textX, textY), Color.Gold);
                textY += 42;
            }
        }

        foreach (var wrappedLine in WrapText(line.Text, panel.Width - 56))
        {
            spriteBatch.DrawString(_font, wrappedLine, new Vector2(textX, textY), Color.White);
            textY += _font.LineSpacing + 4;
        }

        var hint = _lineIndex == _activeSequence.Lines.Count - 1
            ? "ENTER / SPACE 继续"
            : "ENTER / SPACE 继续    ESC 跳过";
        var hintSize = _font.MeasureString(StripUnsupportedCharacters(hint));
        spriteBatch.DrawString(
            _font,
            StripUnsupportedCharacters(hint),
            new Vector2(panel.Right - hintSize.X - 24, panel.Bottom - hintSize.Y - 16),
            Color.LightGray);
    }

    private IEnumerable<string> WrapText(string text, float maxWidth)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }

        var safeText = StripUnsupportedCharacters(text);
        var currentLine = new StringBuilder();
        foreach (var character in safeText)
        {
            if (!IsCharacterSupported(character))
            {
                continue;
            }

            var candidate = currentLine.ToString() + character;
            if (currentLine.Length > 0 && SafeMeasure(candidate) > maxWidth)
            {
                yield return currentLine.ToString();
                currentLine.Clear();
                currentLine.Append(character);
            }
            else
            {
                currentLine.Append(character);
            }
        }

        if (currentLine.Length > 0)
        {
            yield return currentLine.ToString();
        }
    }

    private string StripUnsupportedCharacters(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (IsCharacterSupported(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private bool IsCharacterSupported(char character)
    {
        if (character == '\r' || character == '\n')
        {
            return true;
        }

        var sample = character.ToString();
        try
        {
            _font.MeasureString(sample);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private float SafeMeasure(string text)
    {
        try
        {
            return _font.MeasureString(text).X;
        }
        catch (ArgumentException)
        {
            return float.MaxValue;
        }
    }

    private static Dictionary<string, StorySequence> LoadSequences(string directory)
    {
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"剧情目录不存在：{directory}");
        }

        var sequences = new Dictionary<string, StorySequence>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in Directory.EnumerateFiles(directory, "*.json"))
        {
            var sequence = JsonSerializer.Deserialize<StorySequence>(File.ReadAllText(path), JsonOptions)
                ?? throw new InvalidDataException($"剧情文件为空或格式无效：{path}");
            if (string.IsNullOrWhiteSpace(sequence.Id) ||
                sequence.Lines == null ||
                sequence.Lines.Count == 0)
            {
                throw new InvalidDataException($"剧情必须包含 id 和至少一条台词：{path}");
            }

            if (sequence.Lines.Any(line => line == null || string.IsNullOrWhiteSpace(line.Text)))
            {
                throw new InvalidDataException($"剧情台词不能为空：{path}");
            }

            if (!sequences.TryAdd(sequence.Id, sequence))
            {
                throw new InvalidDataException($"剧情 ID 重复：{sequence.Id}");
            }
        }

        if (sequences.Count == 0)
        {
            throw new InvalidDataException($"剧情目录中没有 JSON 剧情文件：{directory}");
        }

        return sequences;
    }
}
