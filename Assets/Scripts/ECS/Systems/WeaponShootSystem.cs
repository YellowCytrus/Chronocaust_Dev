using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Chronocaust.Ecs.Systems
{
    public sealed class WeaponShootSystem : IEcsUpdateSystem
    {
        public void Update(EcsWorld world, float deltaTime)
        {
            List<(Vector3 position, Vector2 direction, WeaponComponent weapon)> pendingShots =
                new List<(Vector3 position, Vector2 direction, WeaponComponent weapon)>();

            foreach (EcsEntity entity in world.Entities)
            {
                if (!entity.Has<PlayerTagComponent>() ||
                    !entity.TryGet(out TransformComponent ownerTransform) ||
                    !entity.TryGet(out InputStateComponent inputState) ||
                    !entity.TryGet(out AimComponent aimComponent) ||
                    !entity.TryGet(out WeaponComponent weaponComponent) ||
                    !entity.TryGet(out WeaponCooldownComponent cooldownComponent) ||
                    ownerTransform.Transform == null)
                {
                    continue;
                }

                if (!inputState.FirePressed || Time.time < cooldownComponent.NextShotTime)
                {
                    continue;
                }

                Vector2 shootDirection = aimComponent.Direction.sqrMagnitude > 0.0001f
                    ? aimComponent.Direction
                    : Vector2.right;

                Vector2 rotatedOffset = RotateVector(weaponComponent.MuzzleOffset, shootDirection);
                Vector3 spawnPosition = ownerTransform.Transform.position + (Vector3)rotatedOffset;
                pendingShots.Add((spawnPosition, shootDirection, weaponComponent));

                float cooldown = weaponComponent.FireRate > 0f ? 1f / weaponComponent.FireRate : 0.1f;
                cooldownComponent.NextShotTime = Time.time + cooldown;
            }

            foreach ((Vector3 position, Vector2 direction, WeaponComponent weapon) shot in pendingShots)
            {
                SpawnProjectile(world, shot.position, shot.direction, shot.weapon);
            }
        }

        private static void SpawnProjectile(EcsWorld world, Vector3 position, Vector2 direction, WeaponComponent weaponComponent)
        {
            EcsEntity projectile = world.CreateEntity();
            GameObject projectileObject = new GameObject("Projectile");
            projectileObject.transform.position = position;
            projectileObject.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

            SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
            renderer.sprite = weaponComponent.ProjectileSprite != null
                ? weaponComponent.ProjectileSprite
                : weaponComponent.WeaponSprite;
            renderer.sortingOrder = 90;

            projectile.Add(new TransformComponent
            {
                Transform = projectileObject.transform
            });

            projectile.Add(new ProjectileComponent
            {
                Direction = direction,
                Speed = weaponComponent.ProjectileSpeed,
                TimeLeft = weaponComponent.ProjectileLifetime
            });
        }

        private static Vector2 RotateVector(Vector2 localOffset, Vector2 forward)
        {
            float angle = Mathf.Atan2(forward.y, forward.x);
            float sin = Mathf.Sin(angle);
            float cos = Mathf.Cos(angle);
            return new Vector2(
                localOffset.x * cos - localOffset.y * sin,
                localOffset.x * sin + localOffset.y * cos
            );
        }
    }
}
