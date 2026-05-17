using Chronocaust.Ecs.Components;
using UnityEngine;

namespace Chronocaust.Ecs
{
    public static class MeleeDataSetup
    {
        public static MeleeDataComponent FromWeaponDefinition(WeaponDefinition def)
        {
            var d = new MeleeDataComponent
            {
                MotionType = def.MeleeMotionType,
                Range = def.MeleeRange,
                ArcAngle = def.MeleeArcAngle,
                StartupDuration = def.MeleeStartupDuration,
                ActiveDuration = def.MeleeActiveDuration,
                RecoveryDuration = def.MeleeRecoveryDuration,
                HitWindowStartT = def.MeleeHitWindowStartT,
                HitWindowEndT = def.MeleeHitWindowEndT,
                AnticipationPull = def.MeleeAnticipationPull,
                ThrustDistance = def.MeleeThrustDistance,
                ThrustHitRadius = def.MeleeThrustHitRadius,
                ThrustVisualTiltMaxDeg = def.MeleeThrustVisualTiltMaxDeg,
                SlamWindupDeg = def.MeleeSlamWindupDeg,
                SlamDownDeg = def.MeleeSlamDownDeg,
                SlamWindupOffsetY = def.MeleeSlamWindupOffsetY,
                SlamStrikeDepth = def.MeleeSlamStrikeDepth,
                SlamUseFixedAimDir = def.MeleeSlamUseFixedAimDir,
                SlamPoseOffsetRight = def.MeleeSlamPoseOffsetRight,
                SlamPoseRotRight = def.MeleeSlamPoseRotRight,
                SlamPoseOffsetLeft = def.MeleeSlamPoseOffsetLeft,
                SlamPoseRotLeft = def.MeleeSlamPoseRotLeft,
                SlamShootEffectOffsetRight = def.MeleeSlamShootEffectOffsetRight,
                SlamShootEffectOffsetLeft = def.MeleeSlamShootEffectOffsetLeft,
                SlamHitSideOffset = def.MeleeSlamHitSideOffset,
                SpinTurns = def.MeleeSpinTurns,
                MeleeViewSuppressFlipY = def.MeleeViewSuppressFlipY,
                MeleeIdleVisualAimSmoothHz = def.MeleeIdleVisualAimSmoothHz
            };
            ApplyDefaults(ref d);
            return d;
        }

        public static MeleeDataComponent FromWeaponComponent(in WeaponComponent w)
        {
            var d = new MeleeDataComponent
            {
                MotionType = w.MeleeMotionType,
                Range = w.MeleeRange,
                ArcAngle = w.MeleeArcAngle,
                StartupDuration = w.MeleeStartupDuration,
                ActiveDuration = w.MeleeActiveDuration,
                RecoveryDuration = w.MeleeRecoveryDuration,
                HitWindowStartT = w.MeleeHitWindowStartT,
                HitWindowEndT = w.MeleeHitWindowEndT,
                AnticipationPull = w.MeleeAnticipationPull,
                ThrustDistance = w.MeleeThrustDistance,
                ThrustHitRadius = w.MeleeThrustHitRadius,
                ThrustVisualTiltMaxDeg = w.MeleeThrustVisualTiltMaxDeg,
                SlamWindupDeg = w.MeleeSlamWindupDeg,
                SlamDownDeg = w.MeleeSlamDownDeg,
                SlamWindupOffsetY = w.MeleeSlamWindupOffsetY,
                SlamStrikeDepth = w.MeleeSlamStrikeDepth,
                SlamUseFixedAimDir = w.MeleeSlamUseFixedAimDir,
                SlamPoseOffsetRight = w.MeleeSlamPoseOffsetRight,
                SlamPoseRotRight = w.MeleeSlamPoseRotRight,
                SlamPoseOffsetLeft = w.MeleeSlamPoseOffsetLeft,
                SlamPoseRotLeft = w.MeleeSlamPoseRotLeft,
                SlamShootEffectOffsetRight = w.MeleeSlamShootEffectOffsetRight,
                SlamShootEffectOffsetLeft = w.MeleeSlamShootEffectOffsetLeft,
                SlamHitSideOffset = w.MeleeSlamHitSideOffset,
                SpinTurns = w.MeleeSpinTurns,
                MeleeViewSuppressFlipY = w.MeleeViewSuppressFlipY,
                MeleeIdleVisualAimSmoothHz = w.MeleeIdleVisualAimSmoothHz
            };
            ApplyDefaults(ref d);
            return d;
        }

        public static void ApplyDefaults(ref MeleeDataComponent d)
        {
            if (d.StartupDuration <= 0f && d.ActiveDuration <= 0f && d.RecoveryDuration <= 0f)
            {
                d.StartupDuration = 0.1f;
                d.ActiveDuration = 0.14f;
                d.RecoveryDuration = 0.1f;
            }

            if (d.Range <= 0f) d.Range = 1.5f;
            if (d.ArcAngle <= 0f) d.ArcAngle = 90f;

            if (d.HitWindowEndT <= d.HitWindowStartT || d.HitWindowEndT <= 0f)
            {
                switch (d.MotionType)
                {
                    case MeleeMotionType.Arc:
                        d.HitWindowStartT = 0.4f;
                        d.HitWindowEndT = 0.7f;
                        break;
                    case MeleeMotionType.Thrust:
                        d.HitWindowStartT = 0.55f;
                        d.HitWindowEndT = 0.75f;
                        break;
                    case MeleeMotionType.GroundSlam:
                        d.HitWindowStartT = 0.45f;
                        d.HitWindowEndT = 0.62f;
                        break;
                    case MeleeMotionType.Spin:
                        d.HitWindowStartT = 0.05f;
                        d.HitWindowEndT = 0.95f;
                        break;
                }
            }

            if (d.AnticipationPull <= 0f) d.AnticipationPull = 0.12f;
            if (d.ThrustDistance <= 0f) d.ThrustDistance = d.Range;
            if (d.ThrustHitRadius <= 0f) d.ThrustHitRadius = 0.12f;
            if (d.SlamWindupDeg == 0f) d.SlamWindupDeg = 55f;
            if (d.SlamDownDeg == 0f) d.SlamDownDeg = 72f;
            if (d.SlamWindupOffsetY == 0f && d.MotionType == MeleeMotionType.GroundSlam) d.SlamWindupOffsetY = 0.22f;
            if (d.SlamStrikeDepth == 0f && d.MotionType == MeleeMotionType.GroundSlam) d.SlamStrikeDepth = 0.32f;
            if (d.SlamHitSideOffset <= 0f && d.MotionType == MeleeMotionType.GroundSlam) d.SlamHitSideOffset = 0.12f;
            if (d.SpinTurns <= 0f) d.SpinTurns = 1f;
        }
    }
}
