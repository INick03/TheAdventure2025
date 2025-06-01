namespace TheAdventure.Models;

public class GodModePickup : RenderableGameObject
{
    private readonly int _textureId;
    private readonly int _size;
    public GodModePickup(int textureId, (int X, int Y) position, int size = 32)
        : base(null!, position)
    {
        _textureId = textureId;
        _size = size;
    }

    public override void Render(GameRenderer renderer)
    {
        var rect = new Silk.NET.Maths.Rectangle<int>(Position.X, Position.Y, _size, _size);
        renderer.RenderTexture(_textureId, new Silk.NET.Maths.Rectangle<int>(0, 0, _size, _size), rect);
    }
}
