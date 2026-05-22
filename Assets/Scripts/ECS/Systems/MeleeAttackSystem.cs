using System.Collections.Generic;
using Chronocaust.Ecs;
using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using UnityEngine;

namespace Chronocaust.Ecs.Systems
{
    public sealed class MeleeAttackSystem : IEcsUpdateSystem
    {
        private const int MaxHitsTracked = 16;

        private readonly PhysicsEntityRegistry _registry;
        private readonly List<MuzzleFlashSpawnPayload> _pendingFlashes = new List<MuzzleFlashSpawnPayload>(4);
        private static readonly Collider2D[] HitBuffer = new Collider2D[32];

        private EcsWorld _meleeWorld;
        private EntityId _meleeInstigator;
        private float _meleeDamage;

        private EcsQuery<PlayerTagComponent, TransformComponent, InputStateComponent,
            AimComponent, EquippedWeaponComponent, WeaponCooldownComponent,
            MeleeTagComponent, MeleeDataComponent> _query;

        public MeleeAttackSystem(PhysicsEntityRegistry registry)
        {
            _registry = registry;
        }

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

                if (transform.Transform == null || !equipped.HasWeapon)
                {
                    return;
                }

                float totalDuration = MeleeWeaponPose.ComputeTotalDuration(in melee);

                if (melee.AttackActive)
                {
                    melee.AttackElapsed += deltaTime;
                    RefreshPhase(ref melee);
                    float attackT = Mathf.Clamp01(melee.AttackElapsed / totalDuration);

                    if (CanDealHit(in melee, attackT))
                    {
                        Vector2 origin = transform.Transform.position;
                        _meleeWorld = world;
                        _meleeInstigator = id;
                        _meleeDamage = equipped.Damage > 0f ? equipped.Damage : 1f;
                        ProcessHits(ref melee, transform.Transform, origin, ref aim, attackT);
                    }

                    if (melee.AttackElapsed >= totalDuration)
                    {
                        EndAttack(ref melee);
                    }

                    return;
                }

                if (!input.FirePressed || cooldown.CooldownRemaining > 0f)
                {
                    return;
                }

                bool fixedSlam = MeleeWeaponPose.IsFixedSlam(in melee);
                if (fixedSlam)
                {
                    bool facingLeft = aim.Direction.sqrMagnitude > 0.0001f && aim.Direction.x < 0f;
                    BeginFixedSlamAttack(ref melee, facingLeft);
                    SpawnFixedSlamMuzzleFlashIfAny(transform, equipped, ref melee, facingLeft);
                    world.CommandBuffer.RecordShotEvent(id, facingLeft ? Vector2.left : Vector2.right);
                }
                else
                {
                    Vector2 attackDir = aim.Direction.sqrMagnitude > 0.0001f
                        ? aim.Direction.normalized
                        : Vector2.right;
                    BeginDirectionalAttack(ref melee, attackDir);
                    SpawnMuzzleFlashDirectional(transform, equipped, attackDir);
                    world.CommandBuffer.RecordShotEvent(id, attackDir);
                }

                cooldown.CooldownRemaining = 1f / equipped.FireRate;
            });

            int flashCount = _pendingFlashes.Count;
            for (int i = 0; i < flashCount; i++)
            {
                MuzzleFlashSpawnPayload f = _pendingFlashes[i];
                world.CommandBuffer.EnqueueSpawnMuzzleFlash(in f);
            }
        }

        private void SpawnFixedSlamMuzzleFlashIfAny(TransformComponent transform,
            EquippedWeaponComponent equipped, ref MeleeDataComponent melee, bool facingLeft)
        {
            if (equipped.ShootEffectFrames == null || equipped.ShootEffectFrames.Length == 0)
            {
                return;
            }

            Vector2 offset = facingLeft ? melee.SlamShootEffectOffsetLeft : melee.SlamShootEffectOffsetRight;
            if (offset.sqrMagnitude < 0.0001f)
            {
                offset = equipped.ShootEffectMuzzleOffset;
            }

            float rotDeg = facingLeft ? melee.SlamPoseRotLeft : melee.SlamPoseRotRight;
            _pendingFlashes.Add(new MuzzleFlashSpawnPayload
            {
                Position = transform.Transform.position + (Vector3)offset,
                RotationDeg = rotDeg,
                Frames = equipped.ShootEffectFrames,
                FrameDuration = equipped.ShootEffectFrameDuration > 0f ? equipped.ShootEffectFrameDuration : 0.05f,
                Scale = equipped.ShootEffectScale > 0f ? equipped.ShootEffectScale : 1f,
                SortingOrder = equipped.WeaponSortingOrder + 1
            });
        }

        private void SpawnMuzzleFlashDirectional(TransformComponent transform, EquippedWeaponComponent equipped,
            Vector2 attackDir)
        {
            if (equipped.ShootEffectFrames == null || equipped.ShootEffectFrames.Length == 0)
            {
                return;
            }

            float rotDeg = Mathf.Atan2(attackDir.y, attackDir.x) * Mathf.Rad2Deg;
            Vector2 effectOffset = RotateVector(equipped.ShootEffectMuzzleOffset, attackDir);
            _pendingFlashes.Add(new MuzzleFlashSpawnPayload
            {
                Position = transform.Transform.position + (Vector3)effectOffset,
                RotationDeg = rotDeg,
                Frames = equipped.ShootEffectFrames,
                FrameDuration = equipped.ShootEffectFrameDuration > 0f ? equipped.ShootEffectFrameDuration : 0.05f,
                Scale = equipped.ShootEffectScale > 0f ? equipped.ShootEffectScale : 1f,
                SortingOrder = equipped.WeaponSortingOrder + 1
            });
        }

        private static void BeginDirectionalAttack(ref MeleeDataComponent melee, Vector2 attackDir)
        {
            melee.AttackActive = true;
            melee.AttackPhase = MeleeAttackPhase.Startup;
            melee.AttackElapsed = 0f;
            melee.AttackAimDir = attackDir;
            melee.AttackFacingLeft = false;
            melee.HitCount = 0;
        }

        private static void BeginFixedSlamAttack(ref MeleeDataComponent melee, bool facingLeft)
        {
            melee.AttackActive = true;
            melee.AttackPhase = MeleeAttackPhase.Startup;
            melee.AttackElapsed = 0f;
            melee.AttackAimDir = Vector2.zero;
            melee.AttackFacingLeft = facingLeft;
            melee.HitCount = 0;
        }

        private static void EndAttack(ref MeleeDataComponent melee)
        {
            melee.AttackActive = false;
            melee.AttackPhase = MeleeAttackPhase.None;
            melee.AttackElapsed = 0f;
            melee.AttackFacingLeft = false;
            melee.AttackAimDir = Vector2.zero;
            melee.HitCount = 0;
        }

        private static void RefreshPhase(ref MeleeDataComponent melee)
        {
            float su = melee.StartupDuration > 0f ? melee.StartupDuration : 0.1f;
            float ac = melee.ActiveDuration > 0f ? melee.ActiveDuration : 0.12f;
            float e = melee.AttackElapsed;
            if (e < su)
            {
                melee.AttackPhase = MeleeAttackPhase.Startup;
            }
            else if (e < su + ac)
            {
                melee.AttackPhase = MeleeAttackPhase.Active;
            }
            else
            {
                melee.AttackPhase = MeleeAttackPhase.Recovery;
            }
        }

        private static bool CanDealHit(in MeleeDataComponent melee, float attackT)
        {
            return melee.AttackPhase == MeleeAttackPhase.Active
                && attackT >= melee.HitWindowStartT
                && attackT <= melee.HitWindowEndT;
        }

        private void ProcessHits(ref MeleeDataComponent melee, Transform attackerRoot,
            Vector2 origin, ref AimComponent aim, float attackT)
        {
            if (MeleeWeaponPose.IsFixedSlam(in melee))
            {
                ProcessHitsFixedSlam(ref melee, attackerRoot, origin);
                return;
            }

            Vector2 attackDir = melee.AttackAimDir.sqrMagnitude > 0.0001f
                ? melee.AttackAimDir.normalized
                : Vector2.right;

            switch (melee.MotionType)
            {
                case MeleeMotionType.Arc:
                    ProcessHitsArc(ref melee, attackerRoot, origin, attackDir);
                    break;
                case MeleeMotionType.Thrust:
                    ProcessHitsThrust(ref melee, attackerRoot, origin, attackDir, attackT);
                    break;
                case MeleeMotionType.GroundSlam:
                    ProcessHitsDirectionalSlam(ref melee, attackerRoot, origin, attackDir);
                    break;
                case MeleeMotionType.Spin:
                    ProcessHitsSpin(ref melee, attackerRoot, origin);
                    break;
            }
        }

        private void ProcessHitsFixedSlam(ref MeleeDataComponent melee, Transform attackerRoot, Vector2 origin)
        {
            float depth = melee.SlamStrikeDepth > 0f ? melee.SlamStrikeDepth : 0.32f;
            float sideOffset = melee.SlamHitSideOffset > 0f ? melee.SlamHitSideOffset : 0.12f;
            float side = melee.AttackFacingLeft ? -1f : 1f;
            Vector2 slamCenter = origin + new Vector2(side * sideOffset, -depth);
            float radius = melee.Range * 0.52f;

            int hitCount = Physics2D.OverlapCircleNonAlloc(slamCenter, radius, HitBuffer);
            for (int h = 0; h < hitCount; h++)
            {
                Collider2D col = HitBuffer[h];
                if (col == null) continue;
                if (IsSelfCollider(col, attackerRoot)) continue;
                TryRegisterHit(ref melee, col);
            }
        }

        private void ProcessHitsArc(ref MeleeDataComponent melee, Transform attackerRoot,
            Vector2 origin, Vector2 attackDir)
        {
            float range = melee.Range > 0f ? melee.Range : 1.5f;
            float halfArc = (melee.ArcAngle > 0f ? melee.ArcAngle : 90f) * 0.5f;

            int hitCount = Physics2D.OverlapCircleNonAlloc(origin, range, HitBuffer);
            for (int h = 0; h < hitCount; h++)
            {
                Collider2D col = HitBuffer[h];
                if (col == null) continue;
                if (IsSelfCollider(col, attackerRoot)) continue;

                Vector2 toTarget = (Vector2)col.transform.position - origin;
                if (toTarget.sqrMagnitude < 0.0001f) continue;
                toTarget.Normalize();
                float angle = Vector2.Angle(attackDir, toTarget);
                if (angle > halfArc) continue;

                TryRegisterHit(ref melee, col);
            }
        }

        private void ProcessHitsThrust(ref MeleeDataComponent melee, Transform attackerRoot,
            Vector2 origin, Vector2 attackDir, float attackT)
        {
            float thrustDist = melee.ThrustDistance > 0f ? melee.ThrustDistance : melee.Range;
            float radius = melee.ThrustHitRadius > 0f ? melee.ThrustHitRadius : 0.12f;
            float u = Mathf.InverseLerp(melee.HitWindowStartT, melee.HitWindowEndT, attackT);
            float ext = Mathf.Lerp(0.18f, 1f, Mathf.Clamp01(u)) * thrustDist;
            Vector2 tip = origin + attackDir * ext;

            int hitCount = Physics2D.OverlapCircleNonAlloc(tip, radius, HitBuffer);
            for (int h = 0; h < hitCount; h++)
            {
                Collider2D col = HitBuffer[h];
                if (col == null) continue;
                if (IsSelfCollider(col, attackerRoot)) continue;
                TryRegisterHit(ref melee, col);
            }
        }

        private void ProcessHitsDirectionalSlam(ref MeleeDataComponent melee, Transform attackerRoot,
            Vector2 origin, Vector2 attackDir)
        {
            float depth = melee.SlamStrikeDepth > 0f ? melee.SlamStrikeDepth : 0.32f;
            Vector2 slamCenter = origin + Vector2.down * depth + attackDir * 0.12f;
            float radius = melee.Range * 0.52f;

            int hitCount = Physics2D.OverlapCircleNonAlloc(slamCenter, radius, HitBuffer);
            for (int h = 0; h < hitCount; h++)
            {
                Collider2D col = HitBuffer[h];
                if (col == null) continue;
                if (IsSelfCollider(col, attackerRoot)) continue;
                TryRegisterHit(ref melee, col);
            }
        }

        private void ProcessHitsSpin(ref MeleeDataComponent melee, Transform attackerRoot, Vector2 origin)
        {
            float range = melee.Range > 0f ? melee.Range : 1.5f;
            int hitCount = Physics2D.OverlapCircleNonAlloc(origin, range, HitBuffer);
            for (int h = 0; h < hitCount; h++)
            {
                Collider2D col = HitBuffer[h];
                if (col == null) continue;
                if (IsSelfCollider(col, attackerRoot)) continue;
                TryRegisterHit(ref melee, col);
            }
        }

        private static bool IsSelfCollider(Collider2D col, Transform attackerRoot)
        {
            return col.transform == attackerRoot || col.transform.IsChildOf(attackerRoot);
        }

        private void TryRegisterHit(ref MeleeDataComponent melee, Collider2D col)
        {
            int colId = col.GetInstanceID();
            if (HasHitId(in melee, colId)) return;
            if (melee.HitCount >= MaxHitsTracked) return;

            if (!_registry.TryResolve(col, out EntityId target))
            {
                return;
            }

            if (!CombatDamage.Apply(_meleeWorld, _meleeInstigator, target, _meleeDamage))
            {
                return;
            }

            SetHitId(ref melee, melee.HitCount, colId);
            melee.HitCount++;
        }

        private static bool HasHitId(in MeleeDataComponent melee, int instanceId)
        {
            for (int i = 0; i < melee.HitCount; i++)
            {
                if (GetHitId(in melee, i) == instanceId) return true;
            }

            return false;
        }

        private static int GetHitId(in MeleeDataComponent melee, int index)
        {
            switch (index)
            {
                case 0: return melee.HitId0;
                case 1: return melee.HitId1;
                case 2: return melee.HitId2;
                case 3: return melee.HitId3;
                case 4: return melee.HitId4;
                case 5: return melee.HitId5;
                case 6: return melee.HitId6;
                case 7: return melee.HitId7;
                case 8: return melee.HitId8;
                case 9: return melee.HitId9;
                case 10: return melee.HitId10;
                case 11: return melee.HitId11;
                case 12: return melee.HitId12;
                case 13: return melee.HitId13;
                case 14: return melee.HitId14;
                case 15: return melee.HitId15;
                default: return 0;
            }
        }

        private static void SetHitId(ref MeleeDataComponent melee, int index, int value)
        {
            switch (index)
            {
                case 0: melee.HitId0 = value; break;
                case 1: melee.HitId1 = value; break;
                case 2: melee.HitId2 = value; break;
                case 3: melee.HitId3 = value; break;
                case 4: melee.HitId4 = value; break;
                case 5: melee.HitId5 = value; break;
                case 6: melee.HitId6 = value; break;
                case 7: melee.HitId7 = value; break;
                case 8: melee.HitId8 = value; break;
                case 9: melee.HitId9 = value; break;
                case 10: melee.HitId10 = value; break;
                case 11: melee.HitId11 = value; break;
                case 12: melee.HitId12 = value; break;
                case 13: melee.HitId13 = value; break;
                case 14: melee.HitId14 = value; break;
                case 15: melee.HitId15 = value; break;
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
