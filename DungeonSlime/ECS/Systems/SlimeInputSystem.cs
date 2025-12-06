using System;
using System.Linq;
using DungeonSlime.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Input;
using Friflo.Engine.ECS;

namespace DungeonSlime.ECS.Systems;

public sealed class SlimeInputSystem : IUpdateSystem
{
    private readonly KeyboardInfo _keyboard;
    private readonly GamePadInfo _gamePad;
    private readonly Func<bool> _canProcessInput;

    public SlimeInputSystem(KeyboardInfo keyboard, GamePadInfo gamePad, Func<bool> canProcessInput)
    {
        _keyboard = keyboard;
        _gamePad = gamePad;
        _canProcessInput = canProcessInput;
    }

    public void Update(EcsWorld world, GameTime gameTime)
    {
        if (!_canProcessInput())
        {
            return;
        }

        foreach ((_, SlimeComponent slime) in world.Query<SlimeComponent>())
        {
            Vector2 potentialNextDirection = Vector2.Zero;

            if (_keyboard.WasKeyJustPressed(Keys.Up) || _keyboard.WasKeyJustPressed(Keys.W) ||
                _gamePad.WasButtonJustPressed(Buttons.DPadUp) || _gamePad.WasButtonJustPressed(Buttons.LeftThumbstickUp))
            {
                potentialNextDirection = -Vector2.UnitY;
            }
            else if (_keyboard.WasKeyJustPressed(Keys.Down) || _keyboard.WasKeyJustPressed(Keys.S) ||
                     _gamePad.WasButtonJustPressed(Buttons.DPadDown) || _gamePad.WasButtonJustPressed(Buttons.LeftThumbstickDown))
            {
                potentialNextDirection = Vector2.UnitY;
            }
            else if (_keyboard.WasKeyJustPressed(Keys.Left) || _keyboard.WasKeyJustPressed(Keys.A) ||
                     _gamePad.WasButtonJustPressed(Buttons.DPadLeft) || _gamePad.WasButtonJustPressed(Buttons.LeftThumbstickLeft))
            {
                potentialNextDirection = -Vector2.UnitX;
            }
            else if (_keyboard.WasKeyJustPressed(Keys.Right) || _keyboard.WasKeyJustPressed(Keys.D) ||
                     _gamePad.WasButtonJustPressed(Buttons.DPadRight) || _gamePad.WasButtonJustPressed(Buttons.LeftThumbstickRight))
            {
                potentialNextDirection = Vector2.UnitX;
            }

            if (potentialNextDirection != Vector2.Zero && slime.InputBuffer.Count < 2)
            {
                Vector2 validateAgainst = slime.InputBuffer.Count > 0 ? slime.InputBuffer.Last() : slime.Segments[0].Direction;
                if (Vector2.Dot(potentialNextDirection, validateAgainst) >= 0)
                {
                    slime.InputBuffer.Enqueue(potentialNextDirection);
                }
            }
        }
    }
}
