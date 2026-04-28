using System;
using System.Collections.Generic;

namespace Chronocaust.Ecs.Core
{
    /// <summary>
    /// Global registry that assigns a unique integer ID to each component type on first access.
    /// IDs are stable within a single AppDomain run (deterministic access order required between runs).
    /// </summary>
    public static class ComponentTypeRegistry
    {
        private static int _next;
        private static readonly Dictionary<Type, int> _map = new Dictionary<Type, int>(32);
        private static readonly List<Type> _types = new List<Type>(32);

        public static int Register(Type t)
        {
            if (_map.TryGetValue(t, out int existing))
            {
                return existing;
            }

            int id = _next++;
            _map[t] = id;
            _types.Add(t);
            return id;
        }

        public static int Count => _next;

        public static Type TypeAt(int id) => _types[id];
    }

    /// <summary>
    /// Per-type accessor: ComponentTypeId&lt;T&gt;.Value is the stable integer ID for type T.
    /// Access forces registration if the type has not been seen yet.
    /// </summary>
    public static class ComponentTypeId<T> where T : struct, IEcsComponent
    {
        public static readonly int Value = ComponentTypeRegistry.Register(typeof(T));
    }
}
