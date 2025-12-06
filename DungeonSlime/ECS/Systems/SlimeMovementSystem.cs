using System;
using DungeonSlime.ECS.Components;
using Friflo.Engine.ECS;
using Microsoft.Xna.Framework;

namespace DungeonSlime.ECS.Systems;

public sealed class SlimeMovementSystem : IUpdateSystem
{
    private readonly Func<bool> _canMove;

    public SlimeMovementSystem(Func<bool> canMove)
    {
        _canMove = canMove;
    }

    public void Update(EcsWorld world, GameTime gameTime)
    {
        if (!_canMove())
        {
            return;
        }

        foreach ((_, SlimeComponent slime) in world.Query<SlimeComponent>())
        {
            slime.MovementTimer += gameTime.ElapsedGameTime;

            if (slime.InputBuffer.Count > 0)
            {
                slime.NextDirection = slime.InputBuffer.Dequeue();
            }

            if (slime.MovementTimer >= slime.MovementTime)
            {
                slime.MovementTimer -= slime.MovementTime;
                Move(slime);
            }

            slime.MovementProgress = (float)(slime.MovementTimer.TotalSeconds / slime.MovementTime.TotalSeconds);
        }
    }

    private static void Move(SlimeComponent slime)
    {
        SlimeSegment head = slime.Segments[0];
        head.Direction = slime.NextDirection;
        head.At = head.To;
        head.To = head.At + head.Direction * slime.Stride;

        slime.Segments.Insert(0, head);
        slime.Segments.RemoveAt(slime.Segments.Count - 1);

        for (int i = 1; i < slime.Segments.Count; i++)
        {
            SlimeSegment segment = slime.Segments[i];
            if (head.At == segment.At)
            {
                slime.InvokeBodyCollision();
                return;
            }
        }
    }
}
