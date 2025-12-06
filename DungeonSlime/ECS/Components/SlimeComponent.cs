using System;
using System.Collections.Generic;
using DungeonSlime.GameObjects;
using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace DungeonSlime.ECS.Components;

public sealed class SlimeComponent
{
    private static readonly TimeSpan s_movementTime = TimeSpan.FromMilliseconds(200);

    public SlimeComponent(AnimatedSprite sprite)
    {
        Sprite = sprite;
        Segments = new List<SlimeSegment>();
        InputBuffer = new Queue<Vector2>();
    }

    public AnimatedSprite Sprite { get; }

    public List<SlimeSegment> Segments { get; }

    public Queue<Vector2> InputBuffer { get; }

    public float Stride { get; set; }

    public Vector2 NextDirection { get; set; }

    public TimeSpan MovementTimer { get; set; }

    public float MovementProgress { get; set; }

    public event EventHandler? BodyCollision;

    public void Reset(Vector2 startingPosition, float stride)
    {
        Segments.Clear();
        InputBuffer.Clear();

        Stride = stride;
        SlimeSegment head = new SlimeSegment
        {
            At = startingPosition,
            To = startingPosition + new Vector2(stride, 0),
            Direction = Vector2.UnitX
        };

        Segments.Add(head);
        NextDirection = head.Direction;
        MovementTimer = TimeSpan.Zero;
        MovementProgress = 0f;
    }

    public void Grow()
    {
        SlimeSegment tail = Segments[Segments.Count - 1];
        SlimeSegment newTail = new SlimeSegment
        {
            At = tail.To + tail.ReverseDirection * Stride,
            To = tail.At,
            Direction = Vector2.Normalize(tail.At - (tail.To + tail.ReverseDirection * Stride))
        };

        Segments.Add(newTail);
    }

    public Circle GetBounds()
    {
        SlimeSegment head = Segments[0];
        Vector2 pos = Vector2.Lerp(head.At, head.To, MovementProgress);
        return new Circle(
            (int)(pos.X + Sprite.Width * 0.5f),
            (int)(pos.Y + Sprite.Height * 0.5f),
            (int)(Sprite.Width * 0.5f)
        );
    }

    public TimeSpan MovementTime => s_movementTime;

    public void InvokeBodyCollision()
    {
        BodyCollision?.Invoke(this, EventArgs.Empty);
    }
}
