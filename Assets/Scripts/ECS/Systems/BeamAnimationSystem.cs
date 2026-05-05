using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using UnityEngine;

namespace Chronocaust.Ecs.Systems
{
    /// <summary>
    /// Rendering-layer system (Update).
    /// Drives the procedural lightning arc animation on beam entities:
    ///   - Perlin-noise perpendicular wobble per segment (crackling electric arc look)
    ///   - Width pulse on the core line
    ///   - Glow alpha fade as the beam expires
    ///   - Endpoints are always anchored; wobble peaks at the midpoint
    /// </summary>
    public sealed class BeamAnimationSystem : IEcsUpdateSystem
    {
        // Pre-allocated position buffer — sized to match BeamSegments in CommandBuffer.
        private readonly Vector3[] _posBuffer = new Vector3[12];

        private EcsQuery<BeamComponent, TransformComponent> _query;

        public void Update(EcsWorld world, float deltaTime)
        {
            _query ??= world.CreateQuery<BeamComponent, TransformComponent>();

            _query.ForEach((EntityId id,
                ref BeamComponent beam,
                ref TransformComponent _) =>
            {
                if (beam.Core == null) return;

                beam.Age += deltaTime;

                int   segments  = Mathf.Clamp(beam.SegmentCount, 2, _posBuffer.Length);
                float lifeRatio = beam.InitialDuration > 0f
                    ? Mathf.Clamp01(beam.TimeLeft / beam.InitialDuration)
                    : 1f;

                Vector2 dir  = beam.Direction;
                Vector2 perp = new Vector2(-dir.y, dir.x);

                float maxAmplitude = beam.Width * 7f * lifeRatio;
                float noiseTime    = beam.Age * 50f + beam.NoiseSeed;

                BuildArcPositions(segments, beam.Origin, dir, beam.Length,
                    perp, maxAmplitude, noiseTime, _posBuffer);

                float pulseWidth = beam.Width * (0.9f + 0.1f * Mathf.Sin(beam.Age * 2f));

                // Explicitly set positionCount then write each point individually.
                // Avoids the Unity bug where SetPositions(Vector3[]) uses the array's5
                // full length instead of positionCount, collapsing extra points to origin.
                beam.Core.positionCount = segments;
                beam.Core.startWidth    = pulseWidth;
                beam.Core.endWidth      = pulseWidth * 0.3f;
                for (int i = 0; i < segments; i++)
                    beam.Core.SetPosition(i, _posBuffer[i]);

                Color coreStart = Color.Lerp(Color.white, beam.Color, 0.3f);
                coreStart.a = lifeRatio;
                Color coreEnd = beam.Color;
                coreEnd.a = lifeRatio * 0.7f;
                beam.Core.startColor = Color.white;
                beam.Core.endColor   = Color.white;

                if (beam.Glow != null)
                {
                    beam.Glow.positionCount = segments;
                    beam.Glow.startWidth    = pulseWidth * 5f * lifeRatio;
                    beam.Glow.endWidth      = pulseWidth * 2f * lifeRatio;
                    for (int i = 0; i < segments; i++)
                        beam.Glow.SetPosition(i, _posBuffer[i]);

                    Color glowStart = beam.Color;
                    glowStart.a = 0.4f * lifeRatio;
                    Color glowEnd = beam.Color;
                    glowEnd.a = .5f * lifeRatio;
                    beam.Glow.startColor = glowStart;
                    beam.Glow.endColor   = glowEnd;
                }
            });
        }

        /// <summary>
        /// Fills <paramref name="buffer"/> with world-space positions of a procedural arc.
        /// Endpoints are anchored; interior points are offset perpendicularly via Perlin noise.
        /// </summary>
        private static void BuildArcPositions(
            int segments, Vector2 origin, Vector2 dir, float length,
            Vector2 perp, float maxAmplitude, float noiseTime, Vector3[] buffer)
        {
            for (int i = 0; i < segments; i++)
            {
                float t       = (segments > 1) ? (float)i / (segments - 1) : 0f;
                Vector2 base_ = origin + dir * (t * length);

                // Envelope: zero at endpoints, peaks at midpoint.
                float envelope = Mathf.Sin(t * Mathf.PI);

                float n1    = Mathf.PerlinNoise(t * 3f + noiseTime,        0f) * 2f - 1f;
                float n2    = Mathf.PerlinNoise(t * 7f + noiseTime * 1.7f, 0f) * 2f - 1f;
                float noise = n1 * 0.7f + n2 * 0.3f;

                Vector2 offset = perp * (noise * envelope * maxAmplitude);
                buffer[i] = base_ + offset;
            }
        }
    }
}
