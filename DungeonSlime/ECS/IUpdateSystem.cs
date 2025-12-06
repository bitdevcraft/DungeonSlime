using Microsoft.Xna.Framework;

namespace Friflo.Engine.ECS;

public interface IUpdateSystem
{
    void Update(EcsWorld world, GameTime gameTime);
}
