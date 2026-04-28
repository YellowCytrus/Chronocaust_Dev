using System.Collections.Generic;

namespace Chronocaust.Ecs.Core
{
    /// <summary>
    /// Owns all data for entities sharing the same ComponentSignature.
    /// Entities are distributed across ArchetypeChunks; a new chunk is created when the current one is full.
    /// </summary>
    public sealed class Archetype
    {
        public readonly ComponentSignature Signature;
        private readonly List<ArchetypeChunk> _chunks = new List<ArchetypeChunk>(4);

        public IReadOnlyList<ArchetypeChunk> Chunks => _chunks;

        public Archetype(ComponentSignature signature)
        {
            Signature = signature;
            _chunks.Add(new ArchetypeChunk(signature));
        }

        /// <summary>
        /// Allocate a row for a new entity. Returns (chunkIndex, row).
        /// Initialises the slot with the entity id; component values start at default(T).
        /// </summary>
        public (int chunkIdx, int row) AddEntity(EntityId id)
        {
            for (int i = 0; i < _chunks.Count; i++)
            {
                if (_chunks[i].HasRoom)
                {
                    return (i, _chunks[i].AddEntity(id));
                }
            }

            ArchetypeChunk newChunk = new ArchetypeChunk(Signature);
            _chunks.Add(newChunk);
            return (_chunks.Count - 1, newChunk.AddEntity(id));
        }

        /// <summary>
        /// Access a component by reference for the entity at (chunkIdx, row).
        /// </summary>
        public ref T GetComponent<T>(int chunkIdx, int row) where T : struct, IEcsComponent
        {
            return ref _chunks[chunkIdx].GetRef<T>(row);
        }

        /// <summary>
        /// Remove entity at (chunkIdx, row) via swap-back.
        /// Returns the EntityId that was relocated into <paramref name="row"/> (invalid if row was last in chunk).
        /// </summary>
        public EntityId RemoveSwapBack(int chunkIdx, int row)
        {
            return _chunks[chunkIdx].RemoveSwapBack(row);
        }

        /// <summary>
        /// Copy component data from (chunkIdx, row) of this archetype to a slot in another archetype's chunk.
        /// Only components present in both archetypes are copied.
        /// </summary>
        public void CopyDataTo(int chunkIdx, int row, ArchetypeChunk destChunk, int destRow)
        {
            _chunks[chunkIdx].CopyRowTo(row, destChunk, destRow);
        }
    }
}
