using DungeonSlime.ECS.Components;
using Friflo.Engine.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;

namespace DungeonSlime.ECS.Systems;

public sealed class SlimeDrawSystem : IDrawSystem
{
    private readonly SpriteBatch _spriteBatch;

    public SlimeDrawSystem(SpriteBatch spriteBatch)
    {
        _spriteBatch = spriteBatch;
    }

    public void Draw(EcsWorld world, GameTime gameTime)
    {
        foreach ((_, SlimeComponent slime) in world.Query<SlimeComponent>())
        {
            foreach (SlimeSegment segment in slime.Segments)
            {
                Vector2 position = Vector2.Lerp(segment.At, segment.To, slime.MovementProgress);
                slime.Sprite.Draw(_spriteBatch, position);
            }
        }
    }
}
