using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using UnityEngine;

namespace Chronocaust.Ecs
{
    /// <summary>Shared weapon slot / ground spawn / archetype tag helpers for inventory systems.</summary>
    public static class WeaponInventoryUtility
    {
        public const float PickupRadius = 1.5f;
        public const float DropForwardOffset = 0.85f;

        public static ref WeaponSlotEntry GetSlot(ref WeaponLoadoutComponent loadout, int index)
        {
            if (index == 0)
            {
                return ref loadout.Slot0;
            }

            return ref loadout.Slot1;
        }

        public static WeaponSlotEntry GetSlotCopy(in WeaponLoadoutComponent loadout, int index) =>
            index == 0 ? loadout.Slot0 : loadout.Slot1;

        public static WeaponSlotEntry FromWeaponComponent(in WeaponComponent source)
        {
            WeaponSlotEntry entry = new WeaponSlotEntry
            {
                DisplayName = source.DisplayName,
                WeaponSprite = source.WeaponSprite,
                ProjectileSprite = source.ProjectileSprite,
                Damage = source.Damage,
                WeaponSpriteScale = source.WeaponSpriteScale > 0f ? source.WeaponSpriteScale : 1f,
                ProjectileSpriteScale = source.ProjectileSpriteScale > 0f ? source.ProjectileSpriteScale : 1f,
                WeaponSortingOrder = source.WeaponSortingOrder,
                ProjectileSortingOrder = source.ProjectileSortingOrder,
                FireRate = source.FireRate,
                ProjectileSpeed = source.ProjectileSpeed,
                ProjectileLifetime = source.ProjectileLifetime,
                MuzzleOffset = source.MuzzleOffset,
                WeaponVisualBaseRotationDeg = source.WeaponVisualBaseRotationDeg,
                WeaponVisualMirrorX = source.WeaponVisualMirrorX,
                WeaponVisualMirrorY = source.WeaponVisualMirrorY,
                MovementSpeedMultiplier = source.MovementSpeedMultiplier > 0f ? source.MovementSpeedMultiplier : 1f,
                ShootEffectFrames = source.ShootEffectFrames,
                ShootEffectFrameDuration = source.ShootEffectFrameDuration > 0f ? source.ShootEffectFrameDuration : 0.05f,
                ShootEffectScale = source.ShootEffectScale > 0f ? source.ShootEffectScale : 1f,
                ShootEffectMuzzleOffset = source.ShootEffectMuzzleOffset,
                RecoilStrength = source.RecoilStrength,
                RecoilDecayRate = source.RecoilDecayRate > 0f ? source.RecoilDecayRate : 8f,
                Kind = source.Kind,
                PelletCount = source.PelletCount,
                SpreadAngle = source.SpreadAngle,
                BeamDuration = source.BeamDuration,
                BeamWidth = source.BeamWidth,
                BeamColor = source.BeamColor,
                MeleeRange = source.MeleeRange,
                MeleeArcAngle = source.MeleeArcAngle,
                MeleeMotionType = source.MeleeMotionType,
                MeleeStartupDuration = source.MeleeStartupDuration,
                MeleeActiveDuration = source.MeleeActiveDuration,
                MeleeRecoveryDuration = source.MeleeRecoveryDuration,
                MeleeHitWindowStartT = source.MeleeHitWindowStartT,
                MeleeHitWindowEndT = source.MeleeHitWindowEndT,
                MeleeAnticipationPull = source.MeleeAnticipationPull,
                MeleeThrustDistance = source.MeleeThrustDistance,
                MeleeThrustHitRadius = source.MeleeThrustHitRadius,
                MeleeThrustVisualTiltMaxDeg = source.MeleeThrustVisualTiltMaxDeg,
                MeleeSlamWindupDeg = source.MeleeSlamWindupDeg,
                MeleeSlamDownDeg = source.MeleeSlamDownDeg,
                MeleeSlamWindupOffsetY = source.MeleeSlamWindupOffsetY,
                MeleeSlamStrikeDepth = source.MeleeSlamStrikeDepth,
                MeleeSlamUseFixedAimDir = source.MeleeSlamUseFixedAimDir,
                MeleeSlamPoseOffsetRight = source.MeleeSlamPoseOffsetRight,
                MeleeSlamPoseRotRight = source.MeleeSlamPoseRotRight,
                MeleeSlamPoseOffsetLeft = source.MeleeSlamPoseOffsetLeft,
                MeleeSlamPoseRotLeft = source.MeleeSlamPoseRotLeft,
                MeleeSlamShootEffectOffsetRight = source.MeleeSlamShootEffectOffsetRight,
                MeleeSlamShootEffectOffsetLeft = source.MeleeSlamShootEffectOffsetLeft,
                MeleeSlamHitSideOffset = source.MeleeSlamHitSideOffset,
                MeleeSpinTurns = source.MeleeSpinTurns,
                MeleeViewSuppressFlipY = source.MeleeViewSuppressFlipY,
                MeleeIdleVisualAimSmoothHz = source.MeleeIdleVisualAimSmoothHz,
                CooldownRemaining = 0f,
            };
            return entry;
        }

        public static WeaponComponent ToWeaponComponent(in WeaponSlotEntry entry) => new WeaponComponent
        {
            DisplayName = entry.DisplayName,
            WeaponSprite = entry.WeaponSprite,
            ProjectileSprite = entry.ProjectileSprite,
            Damage = entry.Damage,
            WeaponSpriteScale = entry.WeaponSpriteScale,
            ProjectileSpriteScale = entry.ProjectileSpriteScale,
            WeaponSortingOrder = entry.WeaponSortingOrder,
            ProjectileSortingOrder = entry.ProjectileSortingOrder,
            FireRate = entry.FireRate,
            ProjectileSpeed = entry.ProjectileSpeed,
            ProjectileLifetime = entry.ProjectileLifetime,
            MuzzleOffset = entry.MuzzleOffset,
            WeaponVisualBaseRotationDeg = entry.WeaponVisualBaseRotationDeg,
            WeaponVisualMirrorX = entry.WeaponVisualMirrorX,
            WeaponVisualMirrorY = entry.WeaponVisualMirrorY,
            MovementSpeedMultiplier = entry.MovementSpeedMultiplier,
            ShootEffectFrames = entry.ShootEffectFrames,
            ShootEffectFrameDuration = entry.ShootEffectFrameDuration,
            ShootEffectScale = entry.ShootEffectScale,
            ShootEffectMuzzleOffset = entry.ShootEffectMuzzleOffset,
            RecoilStrength = entry.RecoilStrength,
            RecoilDecayRate = entry.RecoilDecayRate,
            Kind = entry.Kind,
            PelletCount = entry.PelletCount,
            SpreadAngle = entry.SpreadAngle,
            BeamDuration = entry.BeamDuration,
            BeamWidth = entry.BeamWidth,
            BeamColor = entry.BeamColor,
            MeleeRange = entry.MeleeRange,
            MeleeArcAngle = entry.MeleeArcAngle,
            MeleeMotionType = entry.MeleeMotionType,
            MeleeStartupDuration = entry.MeleeStartupDuration,
            MeleeActiveDuration = entry.MeleeActiveDuration,
            MeleeRecoveryDuration = entry.MeleeRecoveryDuration,
            MeleeHitWindowStartT = entry.MeleeHitWindowStartT,
            MeleeHitWindowEndT = entry.MeleeHitWindowEndT,
            MeleeAnticipationPull = entry.MeleeAnticipationPull,
            MeleeThrustDistance = entry.MeleeThrustDistance,
            MeleeThrustHitRadius = entry.MeleeThrustHitRadius,
            MeleeThrustVisualTiltMaxDeg = entry.MeleeThrustVisualTiltMaxDeg,
            MeleeSlamWindupDeg = entry.MeleeSlamWindupDeg,
            MeleeSlamDownDeg = entry.MeleeSlamDownDeg,
            MeleeSlamWindupOffsetY = entry.MeleeSlamWindupOffsetY,
            MeleeSlamStrikeDepth = entry.MeleeSlamStrikeDepth,
            MeleeSlamUseFixedAimDir = entry.MeleeSlamUseFixedAimDir,
            MeleeSlamPoseOffsetRight = entry.MeleeSlamPoseOffsetRight,
            MeleeSlamPoseRotRight = entry.MeleeSlamPoseRotRight,
            MeleeSlamPoseOffsetLeft = entry.MeleeSlamPoseOffsetLeft,
            MeleeSlamPoseRotLeft = entry.MeleeSlamPoseRotLeft,
            MeleeSlamShootEffectOffsetRight = entry.MeleeSlamShootEffectOffsetRight,
            MeleeSlamShootEffectOffsetLeft = entry.MeleeSlamShootEffectOffsetLeft,
            MeleeSlamHitSideOffset = entry.MeleeSlamHitSideOffset,
            MeleeSpinTurns = entry.MeleeSpinTurns,
            MeleeViewSuppressFlipY = entry.MeleeViewSuppressFlipY,
            MeleeIdleVisualAimSmoothHz = entry.MeleeIdleVisualAimSmoothHz,
        };

        public static EquippedWeaponComponent ToEquipped(in WeaponSlotEntry entry) =>
            EquippedWeaponComponent.From(ToWeaponComponent(in entry));

        public static WeaponSlotEntry FromEquipped(in EquippedWeaponComponent equipped, WeaponKind kind,
            in ShotgunDataComponent shotgun, in LaserDataComponent laser, in MeleeDataComponent melee,
            float cooldownRemaining)
        {
            WeaponSlotEntry entry = new WeaponSlotEntry
            {
                DisplayName = equipped.DisplayName,
                WeaponSprite = equipped.WeaponSprite,
                ProjectileSprite = equipped.ProjectileSprite,
                Damage = equipped.Damage,
                WeaponSpriteScale = equipped.WeaponSpriteScale,
                ProjectileSpriteScale = equipped.ProjectileSpriteScale,
                WeaponSortingOrder = equipped.WeaponSortingOrder,
                ProjectileSortingOrder = equipped.ProjectileSortingOrder,
                FireRate = equipped.FireRate,
                ProjectileSpeed = equipped.ProjectileSpeed,
                ProjectileLifetime = equipped.ProjectileLifetime,
                MuzzleOffset = equipped.MuzzleOffset,
                WeaponVisualBaseRotationDeg = equipped.WeaponVisualBaseRotationDeg,
                WeaponVisualMirrorX = equipped.WeaponVisualMirrorX,
                WeaponVisualMirrorY = equipped.WeaponVisualMirrorY,
                MovementSpeedMultiplier = equipped.MovementSpeedMultiplier,
                ShootEffectFrames = equipped.ShootEffectFrames,
                ShootEffectFrameDuration = equipped.ShootEffectFrameDuration,
                ShootEffectScale = equipped.ShootEffectScale,
                ShootEffectMuzzleOffset = equipped.ShootEffectMuzzleOffset,
                RecoilStrength = equipped.RecoilStrength,
                RecoilDecayRate = equipped.RecoilDecayRate,
                Kind = kind,
                CooldownRemaining = cooldownRemaining,
            };

            switch (kind)
            {
                case WeaponKind.Shotgun:
                    entry.PelletCount = shotgun.PelletCount;
                    entry.SpreadAngle = shotgun.SpreadAngle;
                    break;
                case WeaponKind.Laser:
                    entry.BeamDuration = laser.BeamDuration;
                    entry.BeamWidth = laser.BeamWidth;
                    entry.BeamColor = laser.BeamColor;
                    break;
                case WeaponKind.Melee:
                    entry.MeleeRange = melee.Range;
                    entry.MeleeArcAngle = melee.ArcAngle;
                    entry.MeleeMotionType = melee.MotionType;
                    entry.MeleeStartupDuration = melee.StartupDuration;
                    entry.MeleeActiveDuration = melee.ActiveDuration;
                    entry.MeleeRecoveryDuration = melee.RecoveryDuration;
                    entry.MeleeHitWindowStartT = melee.HitWindowStartT;
                    entry.MeleeHitWindowEndT = melee.HitWindowEndT;
                    entry.MeleeAnticipationPull = melee.AnticipationPull;
                    entry.MeleeThrustDistance = melee.ThrustDistance;
                    entry.MeleeThrustHitRadius = melee.ThrustHitRadius;
                    entry.MeleeThrustVisualTiltMaxDeg = melee.ThrustVisualTiltMaxDeg;
                    entry.MeleeSlamWindupDeg = melee.SlamWindupDeg;
                    entry.MeleeSlamDownDeg = melee.SlamDownDeg;
                    entry.MeleeSlamWindupOffsetY = melee.SlamWindupOffsetY;
                    entry.MeleeSlamStrikeDepth = melee.SlamStrikeDepth;
                    entry.MeleeSlamUseFixedAimDir = melee.SlamUseFixedAimDir;
                    entry.MeleeSlamPoseOffsetRight = melee.SlamPoseOffsetRight;
                    entry.MeleeSlamPoseRotRight = melee.SlamPoseRotRight;
                    entry.MeleeSlamPoseOffsetLeft = melee.SlamPoseOffsetLeft;
                    entry.MeleeSlamPoseRotLeft = melee.SlamPoseRotLeft;
                    entry.MeleeSlamShootEffectOffsetRight = melee.SlamShootEffectOffsetRight;
                    entry.MeleeSlamShootEffectOffsetLeft = melee.SlamShootEffectOffsetLeft;
                    entry.MeleeSlamHitSideOffset = melee.SlamHitSideOffset;
                    entry.MeleeSpinTurns = melee.SpinTurns;
                    entry.MeleeViewSuppressFlipY = melee.MeleeViewSuppressFlipY;
                    entry.MeleeIdleVisualAimSmoothHz = melee.MeleeIdleVisualAimSmoothHz;
                    break;
            }

            return entry;
        }

        public static WeaponKind ResolveKind(EcsWorld world, EntityId id, ComponentSignature sig)
        {
            if (sig.Has<MeleeTagComponent>()) return WeaponKind.Melee;
            if (sig.Has<LaserTagComponent>()) return WeaponKind.Laser;
            if (sig.Has<ShotgunTagComponent>()) return WeaponKind.Shotgun;
            return WeaponKind.Default;
        }

        public static void SaveActiveSlotFromEntity(EcsWorld world, EntityId id, ref WeaponLoadoutComponent loadout)
        {
            ComponentSignature sig = world.GetSignature(id);
            WeaponKind kind = ResolveKind(world, id, sig);
            ref EquippedWeaponComponent equipped = ref world.GetComponent<EquippedWeaponComponent>(id);
            float cooldown = world.GetComponent<WeaponCooldownComponent>(id).CooldownRemaining;

            ShotgunDataComponent shotgun = default;
            LaserDataComponent laser = default;
            MeleeDataComponent melee = default;
            if (kind == WeaponKind.Shotgun) shotgun = world.GetComponent<ShotgunDataComponent>(id);
            else if (kind == WeaponKind.Laser) laser = world.GetComponent<LaserDataComponent>(id);
            else if (kind == WeaponKind.Melee) melee = world.GetComponent<MeleeDataComponent>(id);

            ref WeaponSlotEntry slot = ref GetSlot(ref loadout, loadout.ActiveIndex);
            slot = equipped.HasWeapon
                ? FromEquipped(in equipped, kind, in shotgun, in laser, in melee, cooldown)
                : WeaponSlotEntry.Empty;
        }

        public static void ApplySlotToEntity(EcsWorld world, EntityId id, ref WeaponLoadoutComponent loadout, int slotIndex)
        {
            loadout.ActiveIndex = Mathf.Clamp(slotIndex, 0, WeaponLoadoutComponent.SlotCount - 1);
            WeaponSlotEntry entry = GetSlotCopy(in loadout, loadout.ActiveIndex);
            ComponentSignature oldSig = world.GetSignature(id);

            RemoveWeaponTypeTags(world, id, oldSig);

            ref EquippedWeaponComponent equipped = ref world.GetComponent<EquippedWeaponComponent>(id);
            if (entry.HasWeapon)
            {
                equipped = ToEquipped(in entry);
                AddWeaponTypeTags(world, id, in entry);
            }
            else
            {
                equipped = default;
            }

            world.GetComponent<WeaponCooldownComponent>(id).CooldownRemaining = entry.CooldownRemaining;
        }

        public static int FindFirstEmptySlot(in WeaponLoadoutComponent loadout)
        {
            if (!loadout.Slot0.HasWeapon) return 0;
            if (!loadout.Slot1.HasWeapon) return 1;
            return -1;
        }

        public static void RemoveWeaponTypeTags(EcsWorld world, EntityId id, ComponentSignature sig)
        {
            if (sig.Has<ShotgunTagComponent>())
            {
                world.CommandBuffer.RemoveComponent<ShotgunTagComponent>(id);
                world.CommandBuffer.RemoveComponent<ShotgunDataComponent>(id);
            }
            else if (sig.Has<LaserTagComponent>())
            {
                world.CommandBuffer.RemoveComponent<LaserTagComponent>(id);
                world.CommandBuffer.RemoveComponent<LaserDataComponent>(id);
            }
            else if (sig.Has<MeleeTagComponent>())
            {
                world.CommandBuffer.RemoveComponent<MeleeTagComponent>(id);
                world.CommandBuffer.RemoveComponent<MeleeDataComponent>(id);
            }
        }

        public static void AddWeaponTypeTags(EcsWorld world, EntityId id, in WeaponSlotEntry entry)
        {
            switch (entry.Kind)
            {
                case WeaponKind.Shotgun:
                    world.CommandBuffer.AddComponent(id, new ShotgunTagComponent());
                    world.CommandBuffer.AddComponent(id, new ShotgunDataComponent
                    {
                        PelletCount = entry.PelletCount > 0 ? entry.PelletCount : 8,
                        SpreadAngle = entry.SpreadAngle
                    });
                    break;
                case WeaponKind.Laser:
                    world.CommandBuffer.AddComponent(id, new LaserTagComponent());
                    world.CommandBuffer.AddComponent(id, new LaserDataComponent
                    {
                        BeamDuration = entry.BeamDuration > 0f ? entry.BeamDuration : 0.3f,
                        BeamWidth = entry.BeamWidth > 0f ? entry.BeamWidth : 0.1f,
                        BeamColor = entry.BeamColor
                    });
                    break;
                case WeaponKind.Melee:
                    world.CommandBuffer.AddComponent(id, new MeleeTagComponent());
                    world.CommandBuffer.AddComponent(id, MeleeDataSetup.FromWeaponComponent(ToWeaponComponent(in entry)));
                    break;
            }
        }

        public static void AddWeaponTypeTags(EcsWorld world, EntityId id, in WeaponComponent weapon)
        {
            AddWeaponTypeTags(world, id, FromWeaponComponent(in weapon));
        }

        public static Vector2 ComputeDropPosition(in TransformComponent transform, in AimComponent aim)
        {
            Vector2 forward = aim.Direction.sqrMagnitude > 0.0001f ? aim.Direction.normalized : Vector2.right;
            Vector2 origin = transform.Transform.position;
            return origin + forward * DropForwardOffset;
        }
    }
}
