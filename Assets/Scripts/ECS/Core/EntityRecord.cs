namespace Chronocaust.Ecs.Core
{
    /// <summary>
    /// Locates an entity's data: which Archetype it belongs to and the exact (chunk, row) slot.
    /// Indexed by EntityId.Index; updated on every migration and swap-back removal.
    /// </summary>
    internal struct EntityRecord
    {
        public Archetype Archetype;
        public int ChunkIndex;
        public int Row;
    }
}
