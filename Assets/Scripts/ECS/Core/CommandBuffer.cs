using System.Collections.Generic;
using Chronocaust.Ecs.Components;
using UnityEngine;

namespace Chronocaust.Ecs.Core
{
    /// <summary>
    /// Accumulates structural ECS operations during a frame; applies them via Playback after iteration ends.
    /// All structural changes trigger archetype migration — no per-component global storage.
    ///
    ///   AddComponent    entity moves Archetype X → Archetype X+T (gains the component)
    ///   RemoveComponent entity moves Archetype X → Archetype X-T (loses the component)
    ///   DestroyEntity   entity removed from its chunk via swap-back
    ///   SpawnProjectile convenience path that builds a full projectile entity
    /// </summary>
    public sealed class CommandBuffer
    {
        private enum OpKind { AddComponent, RemoveComponent, Destroy }

        private readonly struct Op
        {
            public readonly OpKind Kind;
            public readonly EntityId Id;
            public readonly int TypeId;
            public readonly object BoxedValue;

            public Op(OpKind kind, EntityId id, int typeId, object boxed)
            {
                Kind = kind; Id = id; TypeId = typeId; BoxedValue = boxed;
            }
        }

        private readonly List<Op> _ops = new List<Op>(16);
        private readonly List<ProjectileSpawnPayload> _projectileSpawns = new List<ProjectileSpawnPayload>(8);

        // -----------------------------------------------------------------------
        // Public API
        // -----------------------------------------------------------------------

        public void AddComponent<T>(EntityId id, in T value) where T : struct, IEcsComponent
        {
            _ops.Add(new Op(OpKind.AddComponent, id, ComponentTypeId<T>.Value, BoxValue(value)));
        }

        public void RemoveComponent<T>(EntityId id) where T : struct, IEcsComponent
        {
            _ops.Add(new Op(OpKind.RemoveComponent, id, ComponentTypeId<T>.Value, null));
        }

        public void DestroyEntity(EntityId id)
        {
            _ops.Add(new Op(OpKind.Destroy, id, -1, null));
        }

        public void EnqueueSpawnProjectile(in ProjectileSpawnPayload payload)
        {
            _projectileSpawns.Add(payload);
        }

        // -----------------------------------------------------------------------
        // Playback — called by EcsWorld at end of frame
        // -----------------------------------------------------------------------

        public void Playback(EcsWorld world)
        {
            foreach (Op op in _ops)
            {
                if (!world.IsAlive(op.Id)) continue;

                switch (op.Kind)
                {
                    case OpKind.AddComponent:
                    {
                        ComponentSignature current = world.GetSignature(op.Id);
                        ComponentSignature next = current.WithTypeId(op.TypeId);
                        if (next != current)
                        {
                            world.MigrateEntityImmediate(op.Id, next, op.TypeId, op.BoxedValue);
                        }

                        break;
                    }

                    case OpKind.RemoveComponent:
                    {
                        ComponentSignature current = world.GetSignature(op.Id);
                        ComponentSignature next = current.WithoutTypeId(op.TypeId);
                        if (next != current)
                        {
                            world.MigrateEntityImmediate(op.Id, next, -1, null);
                        }

                        break;
                    }

                    case OpKind.Destroy:
                        world.DestroyEntity(op.Id);
                        break;
                }
            }

            _ops.Clear();

            int count = _projectileSpawns.Count;
            for (int i = 0; i < count; i++)
            {
                ProjectileSpawnPayload payload = _projectileSpawns[i];
                SpawnProjectileImmediate(world, payload);
            }

            _projectileSpawns.Clear();
        }

        // -----------------------------------------------------------------------
        // Private helpers
        // -----------------------------------------------------------------------

        private static void SpawnProjectileImmediate(EcsWorld world, ProjectileSpawnPayload p)
        {
            // Unity-object creation lives here at the presentation boundary, not inside Core iteration.
            GameObject go = new GameObject("Projectile");
            go.transform.position = p.Position;
            go.transform.rotation = Quaternion.Euler(
                0f, 0f, Mathf.Atan2(p.Direction.y, p.Direction.x) * Mathf.Rad2Deg);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = p.ProjectileSprite != null ? p.ProjectileSprite : p.FallbackSprite;
            sr.sortingOrder = p.SortingOrder;
            go.transform.localScale = new Vector3(p.ProjectileScale, p.ProjectileScale, 1f);

            ComponentSignature sig = ComponentSignature.Empty
                .With<TransformComponent>()
                .With<ProjectileComponent>();

            EntityId id = world.CreateEntity(sig);
            world.GetComponent<TransformComponent>(id).Transform = go.transform;
            ref ProjectileComponent proj = ref world.GetComponent<ProjectileComponent>(id);
            proj.Direction = p.Direction;
            proj.Speed = p.Speed;
            proj.TimeLeft = p.Lifetime;
        }

        private static object BoxValue<T>(in T value) where T : struct => value;
    }

    /// <summary>Plain data payload for deferred projectile spawning.</summary>
    public struct ProjectileSpawnPayload
    {
        public Vector3 Position;
        public Vector2 Direction;
        public float Speed;
        public float Lifetime;
        public Sprite ProjectileSprite;
        public Sprite FallbackSprite;
        public float ProjectileScale;
        public int SortingOrder;
    }
}
