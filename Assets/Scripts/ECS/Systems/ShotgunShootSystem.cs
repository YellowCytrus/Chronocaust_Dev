using System.Collections.Generic;
using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using UnityEngine;

namespace Chronocaust.Ecs.Systems
{
    public sealed class ShotgunShootSystem : IEcsUpdateSystem
    {
        private readonly List<ProjectileSpawnPayload>  _pending       = new List<ProjectileSpawnPayload>(16);
        private readonly List<MuzzleFlashSpawnPayload> _pendingFlashes = new List<MuzzleFlashSpawnPayload>(4);

        private EcsQuery<PlayerTagComponent, TransformComponent, InputStateComponent,
            AimComponent, EquippedWeaponComponent, WeaponCooldownComponent,
            ShotgunTagComponent, ShotgunDataComponent> _query;

        public void Update(EcsWorld world, float deltaTime)
        {
            _query ??= world.CreateQuery<PlayerTagComponent, TransformComponent, InputStateComponent,
                AimComponent, EquippedWeaponComponent, WeaponCooldownComponent,
                ShotgunTagComponent, ShotgunDataComponent>();

            _pending.Clear();
            _pendingFlashes.Clear();

            _query.ForEach((EntityId id,
                ref PlayerTagComponent _tag,
                ref TransformComponent transform,
                ref InputStateComponent input,
                ref AimComponent aim,
                ref EquippedWeaponComponent equipped,
                ref WeaponCooldownComponent cooldown,
                ref ShotgunTagComponent _shotgunTag,
                ref ShotgunDataComponent shotgun) =>
            {
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
                Vector3 muzzlePos = transform.Transform.position + (Vector3)rotatedOffset;
                float baseAngle = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;

                int pellets = shotgun.PelletCount > 0 ? shotgun.PelletCount : 8;
                float halfSpread = shotgun.SpreadAngle * 0.5f;

                for (int p = 0; p < pellets; p++)
                {
                    // Distribute evenly across the spread cone.
                    float t = pellets > 1 ? (float)p / (pellets - 1) : 0.5f;
                    float angleDeg = baseAngle + Mathf.Lerp(-halfSpread, halfSpread, t);
                    float angleRad = angleDeg * Mathf.Deg2Rad;
                    Vector2 pelletDir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

                    _pending.Add(new ProjectileSpawnPayload
                    {
                        Position       = muzzlePos,
                        Direction      = pelletDir,
                        Speed          = equipped.ProjectileSpeed,
                        Lifetime       = equipped.ProjectileLifetime,
                        ProjectileSprite = equipped.ProjectileSprite,
                        FallbackSprite = equipped.WeaponSprite,
                        ProjectileScale = equipped.ProjectileSpriteScale > 0f ? equipped.ProjectileSpriteScale : 1f,
                        SortingOrder   = equipped.ProjectileSortingOrder
                    });
                }

                if (equipped.ShootEffectFrames != null && equipped.ShootEffectFrames.Length > 0)
                {
                    Vector2 effectOffset = RotateVector(equipped.ShootEffectMuzzleOffset, shootDir);
                    _pendingFlashes.Add(new MuzzleFlashSpawnPayload
                    {
                        Position     = transform.Transform.position + (Vector3)effectOffset,
                        RotationDeg  = baseAngle,
                        Frames       = equipped.ShootEffectFrames,
                        FrameDuration = equipped.ShootEffectFrameDuration > 0f ? equipped.ShootEffectFrameDuration : 0.05f,
                        Scale        = equipped.ShootEffectScale > 0f ? equipped.ShootEffectScale : 1f,
                        SortingOrder = equipped.WeaponSortingOrder + 1
                    });
                }

                world.CommandBuffer.RecordShotEvent(id, shootDir);
                cooldown.CooldownRemaining = 1f / equipped.FireRate;
            });

            int count = _pending.Count;
            for (int i = 0; i < count; i++)
            {
                ProjectileSpawnPayload p = _pending[i];
                world.CommandBuffer.EnqueueSpawnProjectile(in p);
            }

            int flashCount = _pendingFlashes.Count;
            for (int i = 0; i < flashCount; i++)
            {
                MuzzleFlashSpawnPayload f = _pendingFlashes[i];
                world.CommandBuffer.EnqueueSpawnMuzzleFlash(in f);
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
