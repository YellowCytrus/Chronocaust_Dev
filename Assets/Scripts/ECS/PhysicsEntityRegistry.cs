using System.Collections.Generic;
using Chronocaust.Ecs.Core;
using UnityEngine;

namespace Chronocaust.Ecs
{
    /// <summary>
    /// Unity physics bridge: maps Collider2D instance ids to ECS entities.
    /// Owned by composition root (EcsCombatBootstrap), not EcsWorld.
    /// </summary>
    public sealed class PhysicsEntityRegistry
    {
        private readonly Dictionary<int, EntityId> _colliderToEntity = new Dictionary<int, EntityId>(64);
        private readonly Dictionary<EntityId, List<int>> _entityToColliders = new Dictionary<EntityId, List<int>>(32);

        public void RegisterDamageableEntity(EntityId id, GameObject root)
        {
            if (root == null)
            {
                return;
            }

            UnregisterEntity(id);

            Collider2D[] colliders = root.GetComponentsInChildren<Collider2D>(true);
            if (colliders == null || colliders.Length == 0)
            {
                return;
            }

            List<int> colliderIds = new List<int>(colliders.Length);
            _entityToColliders[id] = colliderIds;

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D col = colliders[i];
                if (col == null) continue;

                int colId = col.GetInstanceID();
                _colliderToEntity[colId] = id;
                colliderIds.Add(colId);
            }
        }

        public void UnregisterEntity(EntityId id)
        {
            if (!_entityToColliders.TryGetValue(id, out List<int> colliderIds))
            {
                return;
            }

            for (int i = 0; i < colliderIds.Count; i++)
            {
                _colliderToEntity.Remove(colliderIds[i]);
            }

            _entityToColliders.Remove(id);
        }

        public bool TryResolve(Collider2D col, out EntityId target)
        {
            target = EntityId.Invalid;
            if (col == null)
            {
                return false;
            }

            return _colliderToEntity.TryGetValue(col.GetInstanceID(), out target);
        }
    }
}
