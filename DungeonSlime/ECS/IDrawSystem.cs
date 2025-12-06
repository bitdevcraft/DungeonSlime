using Microsoft.Xna.Framework;

namespace Friflo.Engine.ECS;

public interface IDrawSystem
{
    void Draw(EcsWorld world, GameTime gameTime);
}
