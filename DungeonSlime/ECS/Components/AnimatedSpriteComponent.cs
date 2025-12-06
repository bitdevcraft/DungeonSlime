using MonoGameLibrary.Graphics;

namespace DungeonSlime.ECS.Components;

public sealed class AnimatedSpriteComponent
{
    public AnimatedSpriteComponent(AnimatedSprite sprite)
    {
        Sprite = sprite;
    }

    public AnimatedSprite Sprite { get; }
}
