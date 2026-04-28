using System;

namespace Chronocaust.Ecs.Core
{
    /// <summary>
    /// Versioned entity identifier. Index is the slot in EntityPool; Generation detects stale references.
    /// </summary>
    public readonly struct EntityId : IEquatable<EntityId>
    {
        public readonly int Index;
        public readonly int Generation;

        public EntityId(int index, int generation)
        {
            Index = index;
            Generation = generation;
        }

        public static EntityId Invalid => new EntityId(-1, 0);

        public bool IsValid => Index >= 0;

        public bool Equals(EntityId other) => Index == other.Index && Generation == other.Generation;

        public override bool Equals(object obj) => obj is EntityId other && Equals(other);

        public override int GetHashCode() => Index * 397 ^ Generation;

        public static bool operator ==(EntityId a, EntityId b) => a.Equals(b);
        public static bool operator !=(EntityId a, EntityId b) => !a.Equals(b);

        public override string ToString() => $"Entity({Index}:{Generation})";
    }
}
