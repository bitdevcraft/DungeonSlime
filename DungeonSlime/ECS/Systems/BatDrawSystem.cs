using DungeonSlime.ECS.Components;
using Friflo.Engine.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;

namespace DungeonSlime.ECS.Systems;

public sealed class BatDrawSystem : IDrawSystem
{
    private readonly SpriteBatch _spriteBatch;

    public BatDrawSystem(SpriteBatch spriteBatch)
    {
        _spriteBatch = spriteBatch;
    }

    public void Draw(EcsWorld world, GameTime gameTime)
    {
        foreach ((_, PositionComponent position, AnimatedSpriteComponent sprite) in world.Query<PositionComponent, AnimatedSpriteComponent>())
        {
            sprite.Sprite.Draw(_spriteBatch, position.Value);
        }
    }
}
