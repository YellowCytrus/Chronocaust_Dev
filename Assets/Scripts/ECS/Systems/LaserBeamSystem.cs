using System.Collections.Generic;
using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using UnityEngine;

namespace Chronocaust.Ecs.Systems
{
    public sealed class LaserBeamSystem : IEcsUpdateSystem
    {
        private readonly List<BeamSpawnPayload>        _pendingBeams   = new List<BeamSpawnPayload>(4);
        private readonly List<MuzzleFlashSpawnPayload> _pendingFlashes = new List<MuzzleFlashSpawnPayload>(4);

        private EcsQuery<PlayerTagComponent, TransformComponent, InputStateComponent,
            AimComponent, EquippedWeaponComponent, WeaponCooldownComponent,
            LaserTagComponent, LaserDataComponent> _query;

        // Beam reaches toward the nearest physics collider or falls back to this distance.
        private const float MaxBeamLength = 20f;

        public void Update(EcsWorld world, float deltaTime)
        {
            _query ??= world.CreateQuery<PlayerTagComponent, TransformComponent, InputStateComponent,
                AimComponent, EquippedWeaponComponent, WeaponCooldownComponent,
                LaserTagComponent, LaserDataComponent>();

            _pendingBeams.Clear();
            _pendingFlashes.Clear();

            _query.ForEach((EntityId id,
                ref PlayerTagComponent _tag,
                ref TransformComponent transform,
                ref InputStateComponent input,
                ref AimComponent aim,
                ref EquippedWeaponComponent equipped,
                ref WeaponCooldownComponent cooldown,
                ref LaserTagComponent _laserTag,
                ref LaserDataComponent laser) =>
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
                Vector2 muzzleOffset = RotateVector(equipped.MuzzleOffset, shootDir);
                Vector2 origin = (Vector2)transform.Transform.position + muzzleOffset;
                float baseAngleDeg = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;

                // Offset the ray origin forward to avoid hitting the shooter's own collider.
                const float SkinWidth = 0.5f;
                Vector2 rayStart = origin + shootDir * SkinWidth;
                float length = MaxBeamLength;
                RaycastHit2D hit = Physics2D.Raycast(rayStart, shootDir, MaxBeamLength - SkinWidth);
                if (hit.collider != null && hit.distance > 0f)
                {
                    length = SkinWidth + hit.distance;
                }

                _pendingBeams.Add(new BeamSpawnPayload
                {
                    Origin      = origin,
                    Direction   = shootDir,
                    Duration    = laser.BeamDuration > 0f ? laser.BeamDuration : 0.3f,
                    Length      = length,
                    Width       = laser.BeamWidth > 0f ? laser.BeamWidth : 0.1f,
                    Color       = laser.BeamColor,
                    SortingOrder = equipped.WeaponSortingOrder + 1
                });

                if (equipped.ShootEffectFrames != null && equipped.ShootEffectFrames.Length > 0)
                {
                    Vector2 effectOffset = RotateVector(equipped.ShootEffectMuzzleOffset, shootDir);
                    _pendingFlashes.Add(new MuzzleFlashSpawnPayload
                    {
                        Position     = transform.Transform.position + (Vector3)(effectOffset),
                        RotationDeg  = baseAngleDeg,
                        Frames       = equipped.ShootEffectFrames,
                        FrameDuration = equipped.ShootEffectFrameDuration > 0f ? equipped.ShootEffectFrameDuration : 0.05f,
                        Scale        = equipped.ShootEffectScale > 0f ? equipped.ShootEffectScale : 1f,
                        SortingOrder = equipped.WeaponSortingOrder + 1
                    });
                }

                world.CommandBuffer.RecordShotEvent(id, shootDir);
                cooldown.CooldownRemaining = 1f / equipped.FireRate;
            });

            int beamCount = _pendingBeams.Count;
            for (int i = 0; i < beamCount; i++)
            {
                BeamSpawnPayload b = _pendingBeams[i];
                world.CommandBuffer.EnqueueSpawnBeam(in b);
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
