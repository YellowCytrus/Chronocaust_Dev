using System.Collections.Generic;
using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using UnityEngine;

namespace Chronocaust.Ecs.Systems
{
    public sealed class WeaponShootSystem : IEcsUpdateSystem
    {
        // Pre-allocated — no heap alloc each frame.
        private readonly List<ProjectileSpawnPayload> _pending = new List<ProjectileSpawnPayload>(8);

        private EcsQuery<PlayerTagComponent, TransformComponent, InputStateComponent,
            AimComponent, EquippedWeaponComponent, WeaponCooldownComponent> _query;

        public void Update(EcsWorld world, float deltaTime)
        {
            _query ??= world.CreateQuery<PlayerTagComponent, TransformComponent, InputStateComponent,
                AimComponent, EquippedWeaponComponent, WeaponCooldownComponent>();

            _pending.Clear();

            _query.ForEach((EntityId id,
                ref PlayerTagComponent _,
                ref TransformComponent transform,
                ref InputStateComponent input,
                ref AimComponent aim,
                ref EquippedWeaponComponent equipped,
                ref WeaponCooldownComponent cooldown) =>
            {
                // Decrement relative timer — no Time.time dependency.
                if (cooldown.CooldownRemaining > 0f)
                {
                    cooldown.CooldownRemaining -= deltaTime;
                }

                if (!equipped.HasWeapon ||
                    !input.FirePressed ||
                    cooldown.CooldownRemaining > 0f ||
                    transform.Transform == null)
                {
                    return;
                }

                Vector2 shootDir = aim.Direction.sqrMagnitude > 0.0001f ? aim.Direction : Vector2.right;
                Vector2 rotatedOffset = RotateVector(equipped.MuzzleOffset, shootDir);

                _pending.Add(new ProjectileSpawnPayload
                {
                    Position = transform.Transform.position + (Vector3)rotatedOffset,
                    Direction = shootDir,
                    Speed = equipped.ProjectileSpeed,
                    Lifetime = equipped.ProjectileLifetime,
                    ProjectileSprite = equipped.ProjectileSprite,
                    FallbackSprite = equipped.WeaponSprite,
                    ProjectileScale = equipped.ProjectileSpriteScale > 0f ? equipped.ProjectileSpriteScale : 1f,
                    SortingOrder = equipped.ProjectileSortingOrder
                });

                cooldown.CooldownRemaining = 1f / equipped.FireRate;
            });

            // Queue spawns after iteration (structural changes deferred via CommandBuffer).
            int count = _pending.Count;
            for (int i = 0; i < count; i++)
            {
                ProjectileSpawnPayload p = _pending[i];
                world.CommandBuffer.EnqueueSpawnProjectile(in p);
            }
        }

        private static Vector2 RotateVector(Vector2 local, Vector2 forward)
        {
            float angle = Mathf.Atan2(forward.y, forward.x);
            float sin = Mathf.Sin(angle);
            float cos = Mathf.Cos(angle);
            return new Vector2(local.x * cos - local.y * sin, local.x * sin + local.y * cos);
        }
    }
}
