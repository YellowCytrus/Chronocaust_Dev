using System.Collections.Generic;
using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using UnityEngine;

namespace Chronocaust.Ecs.Systems
{
    public sealed class MeleeAttackSystem : IEcsUpdateSystem
    {
        private readonly List<MuzzleFlashSpawnPayload> _pendingFlashes = new List<MuzzleFlashSpawnPayload>(4);

        // Reusable buffer for Physics2D overlap — avoids per-frame allocation.
        private readonly Collider2D[] _hitBuffer = new Collider2D[32];

        private EcsQuery<PlayerTagComponent, TransformComponent, InputStateComponent,
            AimComponent, EquippedWeaponComponent, WeaponCooldownComponent,
            MeleeTagComponent, MeleeDataComponent> _query;

        public void Update(EcsWorld world, float deltaTime)
        {
            _query ??= world.CreateQuery<PlayerTagComponent, TransformComponent, InputStateComponent,
                AimComponent, EquippedWeaponComponent, WeaponCooldownComponent,
                MeleeTagComponent, MeleeDataComponent>();

            _pendingFlashes.Clear();

            _query.ForEach((EntityId id,
                ref PlayerTagComponent _tag,
                ref TransformComponent transform,
                ref InputStateComponent input,
                ref AimComponent aim,
                ref EquippedWeaponComponent equipped,
                ref WeaponCooldownComponent cooldown,
                ref MeleeTagComponent _meleeTag,
                ref MeleeDataComponent melee) =>
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

                Vector2 attackDir = aim.Direction.sqrMagnitude > 0.0001f ? aim.Direction : Vector2.right;
                Vector2 origin    = transform.Transform.position;
                float   range     = melee.Range > 0f ? melee.Range : 1.5f;
                float   halfArc   = (melee.ArcAngle > 0f ? melee.ArcAngle : 90f) * 0.5f;

                int hitCount = Physics2D.OverlapCircleNonAlloc(origin, range, _hitBuffer);

                for (int h = 0; h < hitCount; h++)
                {
                    Collider2D col = _hitBuffer[h];
                    if (col == null) continue;

                    // Ignore the attacker's own collider(s).
                    if (col.transform == transform.Transform ||
                        col.transform.IsChildOf(transform.Transform))
                    {
                        continue;
                    }

                    Vector2 toTarget = ((Vector2)col.transform.position - origin).normalized;
                    float   angle    = Vector2.Angle(attackDir, toTarget);

                    if (angle <= halfArc)
                    {
                        // TODO: apply damage component via CommandBuffer once DamageComponent exists.
                        Debug.Log($"[MeleeAttack] Hit: {col.gameObject.name}  angle={angle:F1}°");
                    }
                }

                if (equipped.ShootEffectFrames != null && equipped.ShootEffectFrames.Length > 0)
                {
                    float rotDeg = Mathf.Atan2(attackDir.y, attackDir.x) * Mathf.Rad2Deg;
                    Vector2 effectOffset = RotateVector(equipped.ShootEffectMuzzleOffset, attackDir);
                    _pendingFlashes.Add(new MuzzleFlashSpawnPayload
                    {
                        Position     = transform.Transform.position + (Vector3)(effectOffset),
                        RotationDeg  = rotDeg,
                        Frames       = equipped.ShootEffectFrames,
                        FrameDuration = equipped.ShootEffectFrameDuration > 0f ? equipped.ShootEffectFrameDuration : 0.05f,
                        Scale        = equipped.ShootEffectScale > 0f ? equipped.ShootEffectScale : 1f,
                        SortingOrder = equipped.WeaponSortingOrder + 1
                    });
                }

                world.CommandBuffer.RecordShotEvent(id, attackDir);
                cooldown.CooldownRemaining = 1f / equipped.FireRate;
            });

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
