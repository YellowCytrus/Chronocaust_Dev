using System;
using System.Collections.Generic;

namespace Chronocaust.Ecs.Core
{
    /// <summary>
    /// One contiguous block of component data for a single archetype.
    /// Stores EntityId[] plus one T[] per component type in the archetype signature.
    /// Component arrays are addressed by ComponentTypeId.Value.
    /// Removal uses swap-back to keep arrays dense.
    /// </summary>
    public sealed class ArchetypeChunk
    {
        public const int DefaultCapacity = 128;

        private readonly Dictionary<int, Array> _arrays;  // typeId → T[]
        public EntityId[] Entities;
        public int Count;

        public bool HasRoom => Count < Entities.Length;
        public int Capacity => Entities.Length;

        public ArchetypeChunk(ComponentSignature signature, int capacity = DefaultCapacity)
        {
            Entities = new EntityId[capacity];
            _arrays = new Dictionary<int, Array>(8);
            // Iterate up to 64 bits — ComponentTypeId<T>.Value access guarantees types are registered
            // before their archetype is first created, so ComponentTypeRegistry.Count is already correct here.
            int typeCount = ComponentTypeRegistry.Count;
            for (int i = 0; i < typeCount && i < 64; i++)
            {
                if (signature.HasTypeId(i))
                {
                    _arrays[i] = Array.CreateInstance(ComponentTypeRegistry.TypeAt(i), capacity);
                }
            }
        }

        /// <summary>
        /// Add an entity slot at the end. Returns the row index.
        /// The caller is responsible for setting component values on the returned row.
        /// </summary>
        public int AddEntity(EntityId id)
        {
            int row = Count;
            Entities[row] = id;
            Count++;
            return row;
        }

        /// <summary>
        /// Get a typed reference into the component array at <paramref name="row"/>.
        /// Type T must be part of this chunk's signature.
        /// </summary>
        public ref T GetRef<T>(int row) where T : struct, IEcsComponent
        {
            int typeId = ComponentTypeId<T>.Value;
            if (!_arrays.TryGetValue(typeId, out Array raw))
            {
                throw new InvalidOperationException(
                    $"ArchetypeChunk does not contain component {typeof(T).Name} (id={typeId}). " +
                    "This chunk's archetype signature does not include this type.");
            }

            return ref ((T[])raw)[row];
        }

        /// <summary>
        /// Copy all component data of a single row to a slot in another chunk.
        /// Only copies types that exist in BOTH chunks.
        /// </summary>
        public void CopyRowTo(int fromRow, ArchetypeChunk toChunk, int toRow)
        {
            foreach (KeyValuePair<int, Array> kv in _arrays)
            {
                if (toChunk._arrays.TryGetValue(kv.Key, out Array dest))
                {
                    Array.Copy(kv.Value, fromRow, dest, toRow, 1);
                }
            }
        }

        /// <summary>
        /// Set one component value at <paramref name="row"/> using a boxed value.
        /// Used during migration to write a newly added component.
        /// </summary>
        public void SetBoxed(int typeId, int row, object value)
        {
            if (_arrays.TryGetValue(typeId, out Array arr))
            {
                arr.SetValue(value, row);
            }
        }

        /// <summary>
        /// Swap-back remove: moves last row into <paramref name="row"/>, decrements Count.
        /// Returns the EntityId that was relocated (the one that was last; invalid if row was last).
        /// </summary>
        public EntityId RemoveSwapBack(int row)
        {
            int last = Count - 1;
            EntityId swapped = EntityId.Invalid;
            if (row != last)
            {
                swapped = Entities[last];
                Entities[row] = Entities[last];
                foreach (Array arr in _arrays.Values)
                {
                    Array.Copy(arr, last, arr, row, 1);
                }
            }

            Entities[last] = EntityId.Invalid;
            Count--;
            return swapped;
        }
    }
}
