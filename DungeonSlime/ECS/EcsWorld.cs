using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Friflo.Engine.ECS;

public sealed class EcsWorld
{
    private readonly Dictionary<int, Dictionary<Type, object>> _entityComponents = new();
    private readonly List<IUpdateSystem> _updateSystems = new();
    private readonly List<IDrawSystem> _drawSystems = new();
    private int _nextEntityId = 1;

    public EcsEntity CreateEntity()
    {
        int id = _nextEntityId++;
        _entityComponents[id] = new Dictionary<Type, object>();
        return new EcsEntity(this, id);
    }

    public void AddUpdateSystem(IUpdateSystem system)
    {
        _updateSystems.Add(system ?? throw new ArgumentNullException(nameof(system)));
    }

    public void AddDrawSystem(IDrawSystem system)
    {
        _drawSystems.Add(system ?? throw new ArgumentNullException(nameof(system)));
    }

    public void Update(GameTime gameTime)
    {
        foreach (IUpdateSystem system in _updateSystems)
        {
            system.Update(this, gameTime);
        }
    }

    public void Draw(GameTime gameTime)
    {
        foreach (IDrawSystem system in _drawSystems)
        {
            system.Draw(this, gameTime);
        }
    }

    internal void AddComponent<TComponent>(int entityId, TComponent component)
    {
        if (!_entityComponents.TryGetValue(entityId, out Dictionary<Type, object> components))
        {
            throw new InvalidOperationException($"Entity {entityId} does not exist.");
        }

        components[typeof(TComponent)] = component!;
    }

    internal TComponent GetComponent<TComponent>(int entityId)
    {
        if (!_entityComponents.TryGetValue(entityId, out Dictionary<Type, object> components))
        {
            throw new InvalidOperationException($"Entity {entityId} does not exist.");
        }

        if (!components.TryGetValue(typeof(TComponent), out object value))
        {
            throw new InvalidOperationException($"Entity {entityId} does not contain component {typeof(TComponent).Name}.");
        }

        return (TComponent)value;
    }

    internal bool TryGetComponent<TComponent>(int entityId, out TComponent component)
    {
        component = default!;
        if (!_entityComponents.TryGetValue(entityId, out Dictionary<Type, object> components))
        {
            return false;
        }

        if (components.TryGetValue(typeof(TComponent), out object value))
        {
            component = (TComponent)value;
            return true;
        }

        return false;
    }

    internal bool HasComponent<TComponent>(int entityId)
    {
        return _entityComponents.TryGetValue(entityId, out Dictionary<Type, object> components) &&
               components.ContainsKey(typeof(TComponent));
    }

    public IEnumerable<(EcsEntity Entity, TComponent Component)> Query<TComponent>()
    {
        Type type = typeof(TComponent);
        foreach ((int id, Dictionary<Type, object> components) in _entityComponents)
        {
            if (components.TryGetValue(type, out object value))
            {
                yield return (new EcsEntity(this, id), (TComponent)value);
            }
        }
    }

    public IEnumerable<(EcsEntity Entity, T1 Component1, T2 Component2)> Query<T1, T2>()
    {
        Type type1 = typeof(T1);
        Type type2 = typeof(T2);
        foreach ((int id, Dictionary<Type, object> components) in _entityComponents)
        {
            if (components.TryGetValue(type1, out object value1) &&
                components.TryGetValue(type2, out object value2))
            {
                yield return (new EcsEntity(this, id), (T1)value1, (T2)value2);
            }
        }
    }
}
