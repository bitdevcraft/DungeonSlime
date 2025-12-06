using Microsoft.Xna.Framework.Audio;

namespace DungeonSlime.ECS.Components;

public sealed class BatComponent
{
    public BatComponent(SoundEffect bounceSoundEffect)
    {
        BounceSoundEffect = bounceSoundEffect;
    }

    public SoundEffect BounceSoundEffect { get; }
}
