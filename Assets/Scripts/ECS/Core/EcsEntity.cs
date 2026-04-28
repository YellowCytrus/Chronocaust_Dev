using System;
using System.Collections.Generic;

namespace Chronocaust.Ecs.Core
{
    public sealed class EcsEntity
    {
        private readonly Dictionary<Type, IEcsComponent> _components = new Dictionary<Type, IEcsComponent>();

        public EcsEntity(int id)
        {
            Id = id;
        }

        public int Id { get; }

        public T Add<T>(T component) where T : class, IEcsComponent
        {
            _components[typeof(T)] = component;
            return component;
        }

        public bool Has<T>() where T : class, IEcsComponent
        {
            return _components.ContainsKey(typeof(T));
        }

        public bool TryGet<T>(out T component) where T : class, IEcsComponent
        {
            if (_components.TryGetValue(typeof(T), out IEcsComponent raw))
            {
                component = (T)raw;
                return true;
            }

            component = null;
            return false;
        }

        public T Get<T>() where T : class, IEcsComponent
        {
            return (T)_components[typeof(T)];
        }
    }
}
