using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using UnityEngine;

namespace Chronocaust.Ecs.Systems
{
    /// <summary>
    /// Rendering layer: updates weapon sprite position, rotation, and flip.
    /// Melee idle: optional exponential smoothing of visual aim angle (held object).
    /// Fixed slam: side-based pose (no aim rotation); directional melee uses AttackAimDir.
    /// </summary>
    public sealed class WeaponViewSystem : IEcsUpdateSystem
    {
        private EcsQuery<AimComponent, EquippedWeaponComponent, WeaponViewComponent> _query;
        private EcsQuery<AimComponent, EquippedWeaponComponent, WeaponViewComponent,
            MeleeTagComponent, MeleeDataComponent> _meleeQuery;

        public void Update(EcsWorld world, float deltaTime)
        {
            _meleeQuery ??= world.CreateQuery<AimComponent, EquippedWeaponComponent, WeaponViewComponent,
                MeleeTagComponent, MeleeDataComponent>();

            _meleeQuery.ForEach((EntityId id,
                ref AimComponent aim,
                ref EquippedWeaponComponent equipped,
                ref WeaponViewComponent view,
                ref MeleeTagComponent _meleeTag,
                ref MeleeDataComponent melee) =>
            {
                ApplyWeaponView(ref aim, ref equipped, ref view, ref melee, true, deltaTime);
            });

            _query ??= world.CreateQuery<AimComponent, EquippedWeaponComponent, WeaponViewComponent>()
                .Excluding<MeleeTagComponent>();

            _query.ForEach((EntityId id,
                ref AimComponent aim,
                ref EquippedWeaponComponent equipped,
                ref WeaponViewComponent view) =>
            {
                MeleeDataComponent dummy = default;
                ApplyWeaponView(ref aim, ref equipped, ref view, ref dummy, false, deltaTime);
            });
        }

        private static void ApplyWeaponView(ref AimComponent aim, ref EquippedWeaponComponent equipped,
            ref WeaponViewComponent view, ref MeleeDataComponent melee, bool isMelee, float deltaTime)
        {
            if (view.Transform == null || view.Renderer == null) return;

            if (!equipped.HasWeapon)
            {
                view.Renderer.enabled = false;
                return;
            }

            view.Renderer.enabled = true;
            view.Renderer.sprite = equipped.WeaponSprite;
            view.Renderer.sortingOrder = equipped.WeaponSortingOrder;
            float scale = equipped.WeaponSpriteScale > 0f ? equipped.WeaponSpriteScale : 1f;
            view.Transform.localScale = new Vector3(scale, scale, 1f);

            if (isMelee && melee.AttackActive && MeleeWeaponPose.IsFixedSlam(in melee))
            {
                ApplyFixedSlamWeaponView(ref equipped, ref view, ref melee);
                return;
            }

            Vector2 dir;
            float extraRot = 0f;
            Vector2 extraLocal = Vector2.zero;

            if (isMelee)
            {
                Vector2 liveAim = aim.Direction.sqrMagnitude > 0.0001f ? aim.Direction.normalized : Vector2.right;
                float liveAimDeg = Mathf.Atan2(liveAim.y, liveAim.x) * Mathf.Rad2Deg;
                if (!melee.AttackActive)
                {
                    float hz = melee.MeleeIdleVisualAimSmoothHz;
                    float smoothT = hz <= 0f ? 1f : (1f - Mathf.Exp(-hz * deltaTime));
                    melee.IdleVisualSmoothedAimDeg = Mathf.LerpAngle(melee.IdleVisualSmoothedAimDeg, liveAimDeg, smoothT);
                }

                if (melee.AttackActive && melee.AttackAimDir.sqrMagnitude > 0.0001f)
                {
                    dir = melee.AttackAimDir.normalized;
                }
                else
                {
                    float s = melee.IdleVisualSmoothedAimDeg * Mathf.Deg2Rad;
                    dir = new Vector2(Mathf.Cos(s), Mathf.Sin(s));
                }

                float attackT = melee.AttackActive ? MeleeWeaponPose.ComputeAttackT(in melee) : 0f;
                MeleeWeaponPose.Evaluate(in melee, attackT, out extraRot, out extraLocal);
            }
            else
            {
                dir = aim.Direction.sqrMagnitude > 0.0001f ? aim.Direction.normalized : Vector2.right;
            }

            float aimDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float weaponZDeg = aimDeg + extraRot + equipped.WeaponVisualBaseRotationDeg;

            Vector2 baseOffsetInAim = new Vector2(
                equipped.MuzzleOffset.x + extraLocal.x,
                equipped.MuzzleOffset.y + extraLocal.y);
            Vector2 mountWorld = AimSpaceToWorldDelta(baseOffsetInAim, dir);

            SetWeaponLocalFromWorldPlaneDelta(view.Transform, mountWorld);
            view.Transform.localRotation = Quaternion.Euler(0f, 0f, weaponZDeg);

            view.Renderer.flipX = equipped.WeaponVisualMirrorX;
            bool suppressFlip = isMelee && melee.MeleeViewSuppressFlipY;
            bool aimFlipY = !suppressFlip && dir.x < 0f;
            view.Renderer.flipY = equipped.WeaponVisualMirrorY ^ aimFlipY;
        }

        private static void ApplyFixedSlamWeaponView(ref EquippedWeaponComponent equipped,
            ref WeaponViewComponent view, ref MeleeDataComponent melee)
        {
            float attackT = MeleeWeaponPose.ComputeAttackT(in melee);
            MeleeWeaponPose.EvaluateFixedSlam(in melee, attackT, out float extraRot, out Vector2 animOffset);

            Vector2 basePos;
            float baseRot;
            if (melee.AttackFacingLeft)
            {
                if (melee.SlamPoseOffsetLeft.sqrMagnitude > 0.001f)
                    basePos = melee.SlamPoseOffsetLeft;
                else
                    basePos = new Vector2(-melee.SlamPoseOffsetRight.x, melee.SlamPoseOffsetRight.y);

                if (Mathf.Abs(melee.SlamPoseRotLeft) > 0.01f)
                    baseRot = melee.SlamPoseRotLeft;
                else
                    baseRot = -melee.SlamPoseRotRight;
            }
            else
            {
                basePos = melee.SlamPoseOffsetRight;
                baseRot = melee.SlamPoseRotRight;
            }

            // EvaluateFixedSlam: offset по Y, rot — wind/slam; отдельное зеркало animOffset/extraRot не нужно.
            Vector2 worldDelta = basePos + animOffset;
            float totalAngle = baseRot + extraRot + equipped.WeaponVisualBaseRotationDeg;

            Transform parent = view.Transform.parent;
            if (parent != null && parent.lossyScale.x < 0f)
            {
                worldDelta.x *= -1f;
                totalAngle = -totalAngle;
            }

            SetWeaponLocalFromWorldPlaneDelta(view.Transform, worldDelta);
            view.Transform.localRotation = Quaternion.Euler(0f, 0f, totalAngle);

            view.Renderer.flipX = equipped.WeaponVisualMirrorX ^ melee.AttackFacingLeft;
        }

        private static void SetWeaponLocalFromWorldPlaneDelta(Transform weaponTf, Vector2 worldDeltaXY)
        {
            Transform parent = weaponTf.parent;
            if (parent != null)
            {
                weaponTf.localPosition = parent.InverseTransformDirection(
                    new Vector3(worldDeltaXY.x, worldDeltaXY.y, 0f));
            }
            else
            {
                weaponTf.localPosition = new Vector3(worldDeltaXY.x, worldDeltaXY.y, 0f);
            }
        }

        private static Vector2 AimSpaceToWorldDelta(Vector2 localXY, Vector2 aimDir)
        {
            Vector2 f = aimDir.sqrMagnitude > 0.0001f ? aimDir.normalized : Vector2.right;
            Vector2 r = new Vector2(-f.y, f.x);
            return f * localXY.x + r * localXY.y;
        }
    }
}
