using System;
using System.Collections.Generic;

namespace Chronocaust.Ecs.Core
{
    /// <summary>
    /// Central ECS coordinator.
    /// - Entities live in ArchetypeChunks; their location is tracked in EntityRecord[].
    /// - Structural changes (AddComponent, RemoveComponent, Destroy) are queued and applied
    ///   at the end of Update/FixedUpdate via MigrateEntity.
    /// - No per-component global storage. No EcsEntity class. No Has() in iteration.
    /// </summary>
    public sealed class EcsWorld
    {
        private readonly EntityPool _pool = new EntityPool(256);
        private readonly ArchetypeRegistry _registry = new ArchetypeRegistry();
        private EntityRecord[] _records = new EntityRecord[256];

        private readonly List<IEcsUpdateSystem> _updateSystems = new List<IEcsUpdateSystem>(16);
        private readonly List<IEcsFixedUpdateSystem> _fixedUpdateSystems = new List<IEcsFixedUpdateSystem>(16);

        private readonly List<EntityId> _destroyQueue = new List<EntityId>(32);
        private readonly List<MigrationOp> _migrationQueue = new List<MigrationOp>(32);

        public CommandBuffer CommandBuffer { get; } = new CommandBuffer();

        /// <summary>Fired just before an entity's data is removed from all chunks.</summary>
        public event Action<EntityId> OnEntityDestroyed;

        public ArchetypeRegistry Registry => _registry;

        // -----------------------------------------------------------------------
        // Systems
        // -----------------------------------------------------------------------

        public void AddSystem(IEcsUpdateSystem system) => _updateSystems.Add(system);
        public void AddSystem(IEcsFixedUpdateSystem system) => _fixedUpdateSystems.Add(system);

        // -----------------------------------------------------------------------
        // Update loop
        // -----------------------------------------------------------------------

        public void Update(float deltaTime)
        {
            foreach (IEcsUpdateSystem s in _updateSystems) s.Update(this, deltaTime);
            FlushPendingOps();
        }

        public void FixedUpdate(float deltaTime)
        {
            foreach (IEcsFixedUpdateSystem s in _fixedUpdateSystems) s.FixedUpdate(this, deltaTime);
            FlushPendingOps();
        }

        // -----------------------------------------------------------------------
        // Entity creation
        // -----------------------------------------------------------------------

        /// <summary>
        /// Create an entity with the given signature. All component values start at default(T).
        /// Use GetComponent&lt;T&gt;(id) to fill in data immediately after creation.
        /// </summary>
        public EntityId CreateEntity(ComponentSignature signature)
        {
            EntityId id = _pool.Create();
            EnsureRecordCapacity(id.Index);

            Archetype archetype = _registry.GetOrCreate(signature);
            (int chunkIdx, int row) = archetype.AddEntity(id);

            _records[id.Index] = new EntityRecord
            {
                Archetype = archetype,
                ChunkIndex = chunkIdx,
                Row = row
            };

            return id;
        }

        // -----------------------------------------------------------------------
        // Component access — direct ref into chunk array (no boxing, no lookup table)
        // -----------------------------------------------------------------------

        public ref T GetComponent<T>(EntityId id) where T : struct, IEcsComponent
        {
            AssertAlive(id);
            ref EntityRecord rec = ref _records[id.Index];
            return ref rec.Archetype.GetComponent<T>(rec.ChunkIndex, rec.Row);
        }

        public bool IsAlive(EntityId id) => _pool.IsAlive(id);

        public ComponentSignature GetSignature(EntityId id)
        {
            AssertAlive(id);
            return _records[id.Index].Archetype.Signature;
        }

        // -----------------------------------------------------------------------
        // Structural changes — deferred until end of frame
        // -----------------------------------------------------------------------

        public void AddComponent<T>(EntityId id, in T value) where T : struct, IEcsComponent
        {
            AssertAlive(id);
            ComponentSignature current = _records[id.Index].Archetype.Signature;
            ComponentSignature next = current.With<T>();
            if (next == current) return; // already has component
            _migrationQueue.Add(new MigrationOp(id, next, ComponentTypeId<T>.Value, BoxValue(value)));
        }

        public void RemoveComponent<T>(EntityId id) where T : struct, IEcsComponent
        {
            AssertAlive(id);
            ComponentSignature current = _records[id.Index].Archetype.Signature;
            ComponentSignature next = current.Without<T>();
            if (next == current) return;
            _migrationQueue.Add(new MigrationOp(id, next, -1, null));
        }

        public void DestroyEntity(EntityId id)
        {
            if (!_pool.IsAlive(id)) return;
            _destroyQueue.Add(id);
        }

        // -----------------------------------------------------------------------
        // Query creation
        // -----------------------------------------------------------------------

        public EcsQuery<T1, T2> CreateQuery<T1, T2>()
            where T1 : struct, IEcsComponent
            where T2 : struct, IEcsComponent
        {
            return new EcsQuery<T1, T2>(_registry);
        }

        public EcsQuery<T1, T2, T3> CreateQuery<T1, T2, T3>()
            where T1 : struct, IEcsComponent
            where T2 : struct, IEcsComponent
            where T3 : struct, IEcsComponent
        {
            return new EcsQuery<T1, T2, T3>(_registry);
        }

        public EcsQuery<T1, T2, T3, T4> CreateQuery<T1, T2, T3, T4>()
            where T1 : struct, IEcsComponent
            where T2 : struct, IEcsComponent
            where T3 : struct, IEcsComponent
            where T4 : struct, IEcsComponent
        {
            return new EcsQuery<T1, T2, T3, T4>(_registry);
        }

        public EcsQuery<T1, T2, T3, T4, T5> CreateQuery<T1, T2, T3, T4, T5>()
            where T1 : struct, IEcsComponent
            where T2 : struct, IEcsComponent
            where T3 : struct, IEcsComponent
            where T4 : struct, IEcsComponent
            where T5 : struct, IEcsComponent
        {
            return new EcsQuery<T1, T2, T3, T4, T5>(_registry);
        }

        public EcsQuery<T1, T2, T3, T4, T5, T6> CreateQuery<T1, T2, T3, T4, T5, T6>()
            where T1 : struct, IEcsComponent
            where T2 : struct, IEcsComponent
            where T3 : struct, IEcsComponent
            where T4 : struct, IEcsComponent
            where T5 : struct, IEcsComponent
            where T6 : struct, IEcsComponent
        {
            return new EcsQuery<T1, T2, T3, T4, T5, T6>(_registry);
        }

        // -----------------------------------------------------------------------
        // Internal: migration + flush
        // -----------------------------------------------------------------------

        internal void MigrateEntityImmediate(EntityId id, ComponentSignature newSignature,
            int extraTypeId, object extraValue)
        {
            if (!_pool.IsAlive(id)) return;

            ref EntityRecord src = ref _records[id.Index];
            Archetype srcArchetype = src.Archetype;
            Archetype dstArchetype = _registry.GetOrCreate(newSignature);

            (int dstChunk, int dstRow) = dstArchetype.AddEntity(id);

            // Copy shared components
            ArchetypeChunk srcChunk = srcArchetype.Chunks[src.ChunkIndex];
            ArchetypeChunk destChunk = dstArchetype.Chunks[dstChunk];
            srcChunk.CopyRowTo(src.Row, destChunk, dstRow);

            // Write newly added component value (if any)
            if (extraTypeId >= 0 && extraValue != null)
            {
                destChunk.SetBoxed(extraTypeId, dstRow, extraValue);
            }

            // Remove from source, get the entity that was swapped into the freed row
            EntityId swapped = srcArchetype.RemoveSwapBack(src.ChunkIndex, src.Row);
            if (_pool.IsAlive(swapped))
            {
                _records[swapped.Index].Row = src.Row;
            }

            src = new EntityRecord
            {
                Archetype = dstArchetype,
                ChunkIndex = dstChunk,
                Row = dstRow
            };
        }

        private void FlushPendingOps()
        {
            // Process queued migrations
            int mCount = _migrationQueue.Count;
            for (int i = 0; i < mCount; i++)
            {
                MigrationOp op = _migrationQueue[i];
                if (_pool.IsAlive(op.Id))
                {
                    MigrateEntityImmediate(op.Id, op.NewSignature, op.ExtraTypeId, op.ExtraValue);
                }
            }

            _migrationQueue.Clear();

            // Flush CommandBuffer spawns
            CommandBuffer.Playback(this);

            // Process destroys
            int dCount = _destroyQueue.Count;
            for (int i = 0; i < dCount; i++)
            {
                EntityId id = _destroyQueue[i];
                if (!_pool.IsAlive(id)) continue;

                OnEntityDestroyed?.Invoke(id);

                ref EntityRecord rec = ref _records[id.Index];
                EntityId swapped = rec.Archetype.RemoveSwapBack(rec.ChunkIndex, rec.Row);
                if (_pool.IsAlive(swapped))
                {
                    _records[swapped.Index].Row = rec.Row;
                }

                _pool.Recycle(id);
            }

            _destroyQueue.Clear();
        }

        private void EnsureRecordCapacity(int index)
        {
            if (index < _records.Length) return;
            int newLen = Math.Max(index + 1, _records.Length * 2);
            Array.Resize(ref _records, newLen);
        }

        private static object BoxValue<T>(in T value) where T : struct => value;

        private void AssertAlive(EntityId id)
        {
            if (!_pool.IsAlive(id))
                throw new InvalidOperationException($"Entity {id.Index}:{id.Generation} is not alive.");
        }

        // -----------------------------------------------------------------------
        // Nested types
        // -----------------------------------------------------------------------

        private readonly struct MigrationOp
        {
            public readonly EntityId Id;
            public readonly ComponentSignature NewSignature;
            public readonly int ExtraTypeId;
            public readonly object ExtraValue;

            public MigrationOp(EntityId id, ComponentSignature newSig, int extraTypeId, object extraValue)
            {
                Id = id;
                NewSignature = newSig;
                ExtraTypeId = extraTypeId;
                ExtraValue = extraValue;
            }
        }
    }
}
