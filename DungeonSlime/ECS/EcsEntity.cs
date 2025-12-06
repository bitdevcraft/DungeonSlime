using System;
using System.Collections.Generic;

namespace Friflo.Engine.ECS;

public readonly struct EcsEntity
{
    private readonly EcsWorld _world;

    public int Id { get; }

    internal EcsEntity(EcsWorld world, int id)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        Id = id;
    }

    public void Add<TComponent>(TComponent component)
    {
        _world.AddComponent(Id, component);
    }

    public TComponent Get<TComponent>()
    {
        return _world.GetComponent<TComponent>(Id);
    }

    public bool TryGet<TComponent>(out TComponent component)
    {
        return _world.TryGetComponent(Id, out component);
    }

    public bool Has<TComponent>()
    {
        return _world.HasComponent<TComponent>(Id);
    }
}
