using System;

namespace Chronocaust.Ecs.Core
{
    /// <summary>
    /// Immutable bitmask of component type IDs. Supports up to 64 distinct component types.
    /// Each bit position corresponds to ComponentTypeId&lt;T&gt;.Value.
    /// </summary>
    public readonly struct ComponentSignature : IEquatable<ComponentSignature>
    {
        private readonly ulong _bits;

        private ComponentSignature(ulong bits)
        {
            _bits = bits;
        }

        public static ComponentSignature Empty => new ComponentSignature(0UL);

        public ComponentSignature With<T>() where T : struct, IEcsComponent
        {
            int id = ComponentTypeId<T>.Value;
            if (id >= 64)
            {
                throw new InvalidOperationException(
                    $"ComponentSignature supports up to 64 types. Type {typeof(T).Name} has id={id}.");
            }

            return new ComponentSignature(_bits | (1UL << id));
        }

        public ComponentSignature Without<T>() where T : struct, IEcsComponent
        {
            int id = ComponentTypeId<T>.Value;
            return new ComponentSignature(_bits & ~(1UL << id));
        }

        public bool Has<T>() where T : struct, IEcsComponent
        {
            int id = ComponentTypeId<T>.Value;
            return (_bits & (1UL << id)) != 0;
        }

        public bool HasTypeId(int id)
        {
            if (id < 0 || id >= 64) return false;
            return (_bits & (1UL << id)) != 0;
        }

        public ComponentSignature WithTypeId(int id)
        {
            if (id < 0 || id >= 64) return this;
            return new ComponentSignature(_bits | (1UL << id));
        }

        public ComponentSignature WithoutTypeId(int id)
        {
            if (id < 0 || id >= 64) return this;
            return new ComponentSignature(_bits & ~(1UL << id));
        }

        /// <summary>Returns true if this signature contains all bits of <paramref name="subset"/>.</summary>
        public bool HasAll(ComponentSignature subset) => (_bits & subset._bits) == subset._bits;

        /// <summary>Returns true if this signature contains at least one bit of <paramref name="mask"/>.</summary>
        public bool HasAny(ComponentSignature mask) => mask._bits != 0 && (_bits & mask._bits) != 0;

        public ulong RawBits => _bits;

        public bool Equals(ComponentSignature other) => _bits == other._bits;

        public override bool Equals(object obj) => obj is ComponentSignature s && Equals(s);

        public override int GetHashCode() => _bits.GetHashCode();

        public static bool operator ==(ComponentSignature a, ComponentSignature b) => a._bits == b._bits;

        public static bool operator !=(ComponentSignature a, ComponentSignature b) => a._bits != b._bits;
    }
}
