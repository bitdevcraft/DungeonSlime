using System;
using DungeonSlime.ECS.Components;
using Friflo.Engine.ECS;
using Microsoft.Xna.Framework;

namespace DungeonSlime.ECS.Systems;

public sealed class BatMovementSystem : IUpdateSystem
{
    private readonly Func<bool> _canMove;

    public BatMovementSystem(Func<bool> canMove)
    {
        _canMove = canMove;
    }

    public void Update(EcsWorld world, GameTime gameTime)
    {
        if (!_canMove())
        {
            return;
        }

        foreach ((_, PositionComponent position, VelocityComponent velocity) in world.Query<PositionComponent, VelocityComponent>())
        {
            position.Value += velocity.Value;
        }
    }
}
