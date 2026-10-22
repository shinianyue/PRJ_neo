using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using test.Entities;

namespace test.Story;

public sealed class DialogueNpc
{
    public string Name { get; }
    public Rectangle Bounds { get; }
    public string SequenceId { get; }
    public bool IsActive { get; private set; }
    public Rectangle InteractionBounds => new(
        Bounds.X - 48,
        Bounds.Y - 12,
        Bounds.Width + 96,
        Bounds.Height + 24);

    public DialogueNpc(string name, Rectangle bounds, string sequenceId)
    {
        Name = name ?? string.Empty;
        Bounds = bounds;
        SequenceId = sequenceId ?? string.Empty;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    public bool IsPlayerNearby(Player player)
    {
        return IsActive && InteractionBounds.Intersects(player.Bounds);
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D texture)
    {
        if (!IsActive)
        {
            return;
        }

        var source = new Rectangle(0, 0, texture.Width, 58);
        var origin = new Vector2(texture.Width / 2f, 0f);
        spriteBatch.Draw(
            texture,
            new Vector2(Bounds.Center.X, Bounds.Y),
            source,
            Color.White,
            0f,
            origin,
            1f,
            SpriteEffects.None,
            0f);
    }
}
