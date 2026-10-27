using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace test.World;

public sealed class CutsceneOverlay
{
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;
    private string _title = string.Empty;
    private string _subtitle = string.Empty;
    private float _elapsed;
    private float _duration;

    public bool IsPlaying => _duration > 0f && _elapsed < _duration;
    public bool ReduceMotion { get; set; }

    public CutsceneOverlay(Texture2D pixel, SpriteFont font)
    {
        _pixel = pixel;
        _font = font;
    }

    public void Play(string title, string subtitle, float duration = 2.4f)
    {
        _title = title;
        _subtitle = subtitle;
        _elapsed = 0f;
        _duration = System.Math.Max(0.5f, duration);
    }

    public void Update(GameTime gameTime)
    {
        if (!IsPlaying)
        {
            return;
        }

        _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    public void Draw(SpriteBatch spriteBatch, Rectangle viewport)
    {
        if (!IsPlaying)
        {
            return;
        }

        var progress = System.Math.Clamp(_elapsed / _duration, 0f, 1f);
        var fadeIn = ReduceMotion ? 1f : System.Math.Clamp(progress / 0.25f, 0f, 1f);
        var fadeOut = ReduceMotion ? 1f : 1f - System.Math.Clamp((progress - 0.75f) / 0.25f, 0f, 1f);
        var alpha = System.Math.Clamp((fadeIn * fadeOut) * 220f, 0f, 220f);

        spriteBatch.Draw(_pixel, viewport, new Color(8, 8, 12, (int)alpha));

        var titleSize = _font.MeasureString(_title);
        var subtitleSize = _font.MeasureString(_subtitle);
        var titlePosition = new Vector2(
            viewport.Width / 2f - titleSize.X / 2f,
            viewport.Height * 0.38f);
        var subtitlePosition = new Vector2(
            viewport.Width / 2f - subtitleSize.X / 2f,
            viewport.Height * 0.46f + 28f);

        spriteBatch.DrawString(_font, _title, titlePosition, new Color(245, 215, 140, (int)(255 * fadeIn * fadeOut)));
        spriteBatch.DrawString(_font, _subtitle, subtitlePosition, new Color(230, 230, 230, (int)(220 * fadeIn * fadeOut)));
    }
}
