using UnityEngine;

namespace Chronocaust.Ecs.Components
{
    /// <summary>
    /// Pure pose generation: weapon local offset + extra Z rotation from normalized attack time and melee data.
    /// </summary>
    public static class MeleeWeaponPose
    {
        public static float ComputeTotalDuration(in MeleeDataComponent m)
        {
            float su = m.StartupDuration > 0f ? m.StartupDuration : 0.1f;
            float ac = m.ActiveDuration > 0f ? m.ActiveDuration : 0.12f;
            float re = m.RecoveryDuration > 0f ? m.RecoveryDuration : 0.1f;
            return su + ac + re;
        }

        public static float ComputeAttackT(in MeleeDataComponent m)
        {
            if (!m.AttackActive) return 0f;
            float total = ComputeTotalDuration(in m);
            if (total <= 0.0001f) return 1f;
            return Mathf.Clamp01(m.AttackElapsed / total);
        }

        public static bool IsFixedSlam(in MeleeDataComponent m) =>
            m.MotionType == MeleeMotionType.GroundSlam && m.SlamUseFixedAimDir;

        /// <summary>Side-based slam: world offsets authored for right side (+X). No aim rotation.</summary>
        public static void EvaluateFixedSlam(in MeleeDataComponent m, float attackT,
            out float extraRotDeg, out Vector2 extraWorldOffsetRight)
        {
            extraRotDeg = 0f;
            extraWorldOffsetRight = Vector2.zero;
            if (!m.AttackActive && attackT <= 0f) return;

            float tSu = m.StartupDuration > 0f ? m.StartupDuration : 0.1f;
            float tAc = m.ActiveDuration > 0f ? m.ActiveDuration : 0.12f;
            float tRe = m.RecoveryDuration > 0f ? m.RecoveryDuration : 0.1f;
            float total = tSu + tAc + tRe;
            if (total <= 0.0001f) return;

            float t = Mathf.Clamp01(attackT);
            float tStartupEnd = tSu / total;
            float tActiveEnd = (tSu + tAc) / total;
            float f = ComputeMassEnvelope(t, tStartupEnd, tActiveEnd);
            float fh = Mathf.Clamp01(f);

            float wind = Mathf.Sin((1f - fh) * Mathf.PI * 0.5f) * m.SlamWindupDeg;
            float slam = Mathf.Sin(fh * Mathf.PI * 0.85f) * -m.SlamDownDeg;
            extraRotDeg = wind + slam;
            extraWorldOffsetRight = new Vector2(0f, m.SlamWindupOffsetY * (1f - fh));
        }

        public static void Evaluate(in MeleeDataComponent m, float attackT, out float extraRotDeg, out Vector2 extraLocalOffset)
        {
            extraRotDeg = 0f;
            extraLocalOffset = Vector2.zero;
            if (IsFixedSlam(in m)) return;
            if (!m.AttackActive && attackT <= 0f) return;

            float tSu = m.StartupDuration > 0f ? m.StartupDuration : 0.1f;
            float tAc = m.ActiveDuration > 0f ? m.ActiveDuration : 0.12f;
            float tRe = m.RecoveryDuration > 0f ? m.RecoveryDuration : 0.1f;
            float total = tSu + tAc + tRe;
            if (total <= 0.0001f) return;

            float t = Mathf.Clamp01(attackT);
            float tStartupEnd = tSu / total;
            float tActiveEnd = (tSu + tAc) / total;

            float f = ComputeMassEnvelope(t, tStartupEnd, tActiveEnd);

            float antiPull = m.AnticipationPull;

            switch (m.MotionType)
            {
                case MeleeMotionType.Arc:
                {
                    float halfArc = (m.ArcAngle > 0f ? m.ArcAngle : 90f) * 0.5f;
                    float swing = (f * 2f - 1f) * halfArc;
                    extraRotDeg = swing;
                    float pull = antiPull * (1f - SmoothTowardRest(t, tStartupEnd, tActiveEnd));
                    extraLocalOffset = new Vector2(-pull, 0f);
                    break;
                }
                case MeleeMotionType.Thrust:
                {
                    float reach = m.ThrustDistance > 0f ? m.ThrustDistance : m.Range;
                    float fh = Mathf.Clamp01(f);
                    float ext = fh * reach;
                    float back = (1f - fh) * antiPull;
                    extraLocalOffset = new Vector2(-back + ext, 0f);
                    float wobble = Mathf.Sin(fh * Mathf.PI) * 0.6f;
                    float visualTilt = m.ThrustVisualTiltMaxDeg * Mathf.Sin(fh * Mathf.PI);
                    extraRotDeg = wobble + visualTilt;
                    break;
                }
                case MeleeMotionType.GroundSlam:
                {
                    float fh = Mathf.Clamp01(f);
                    float wind = Mathf.Sin((1f - fh) * Mathf.PI * 0.5f) * m.SlamWindupDeg;
                    float slam = Mathf.Sin(fh * Mathf.PI * 0.85f) * -m.SlamDownDeg;
                    extraRotDeg = wind + slam;
                    extraLocalOffset = new Vector2(0f, m.SlamWindupOffsetY * (1f - fh) + m.SlamStrikeDepth * fh);
                    break;
                }
                case MeleeMotionType.Spin:
                {
                    float turns = m.SpinTurns > 0f ? m.SpinTurns : 1f;
                    float spinEase = Mathf.SmoothStep(0f, 1f, Mathf.SmoothStep(0f, 1f, t));
                    extraRotDeg = turns * 360f * spinEase;
                    extraLocalOffset = Vector2.zero;
                    break;
                }
            }
        }

        /// <summary>Stroke envelope 0..1 with anticipation dip then strike then return.</summary>
        private static float ComputeMassEnvelope(float t, float tStartupEnd, float tActiveEnd)
        {
            const float anticipation = 0.12f;

            if (t <= tStartupEnd)
            {
                float u = tStartupEnd > 0.0001f ? t / tStartupEnd : 1f;
                return -anticipation * Mathf.SmoothStep(0f, 1f, u);
            }

            if (t <= tActiveEnd)
            {
                float span = tActiveEnd - tStartupEnd;
                float u = span > 0.0001f ? (t - tStartupEnd) / span : 1f;
                float travel = ArcTravelSharpMiddle(u);
                return Mathf.Lerp(-anticipation, 1f, travel);
            }

            float spanR = 1f - tActiveEnd;
            float ur = spanR > 0.0001f ? (t - tActiveEnd) / spanR : 1f;
            return Mathf.Lerp(1f, 0f, Mathf.SmoothStep(0f, 1f, ur));
        }

        private static float SmoothTowardRest(float t, float tStartupEnd, float tActiveEnd)
        {
            if (t <= tStartupEnd) return 0f;
            if (t >= tActiveEnd) return 1f;
            return (t - tStartupEnd) / (tActiveEnd - tStartupEnd);
        }

        /// <summary>Slow ends, fast middle — arc mass feel.</summary>
        private static float ArcTravelSharpMiddle(float u)
        {
            const float edge = 0.22f;
            u = Mathf.Clamp01(u);
            if (u <= edge)
                return Mathf.Lerp(0f, 0.18f, u / edge);
            if (u >= 1f - edge)
                return Mathf.Lerp(0.82f, 1f, (u - (1f - edge)) / edge);
            float mid = (u - edge) / (1f - 2f * edge);
            return Mathf.Lerp(0.18f, 0.82f, mid);
        }
    }
}
