using System.Collections.Generic;
using Chronocaust.Ecs.Components;
using Chronocaust.Ecs;
using UnityEngine;

namespace Chronocaust.Ecs.Core
{
    /// <summary>
    /// Accumulates structural ECS operations during a frame; applies them via Playback after iteration ends.
    /// All structural changes trigger archetype migration — no per-component global storage.
    ///
    ///   AddComponent    entity moves Archetype X → Archetype X+T (gains the component)
    ///   RemoveComponent entity moves Archetype X → Archetype X-T (loses the component)
    ///   DestroyEntity   entity removed from its chunk via swap-back
    ///   SpawnProjectile convenience path that builds a full projectile entity
    ///   ShotEvents      plain-data list; written by any shoot system, read by listener systems
    ///                   in the same frame, cleared in Playback — no component pollution.
    /// </summary>
    public sealed class CommandBuffer
    {
        /// <summary>
        /// Lightweight shot event. Written by any weapon system that fires; consumed by
        /// independent systems (recoil, camera shake, audio, …) without any coupling.
        /// </summary>
        public readonly struct ShotEvent
        {
            public readonly EntityId Shooter;
            public readonly UnityEngine.Vector2 Direction;

            public ShotEvent(EntityId shooter, UnityEngine.Vector2 direction)
            {
                Shooter   = shooter;
                Direction = direction;
            }
        }
        private enum OpKind { AddComponent, RemoveComponent, Destroy }

        private readonly struct Op
        {
            public readonly OpKind Kind;
            public readonly EntityId Id;
            public readonly int TypeId;
            public readonly object BoxedValue;

            public Op(OpKind kind, EntityId id, int typeId, object boxed)
            {
                Kind = kind; Id = id; TypeId = typeId; BoxedValue = boxed;
            }
        }

        private readonly List<Op> _ops = new List<Op>(16);
        private readonly List<ProjectileSpawnPayload> _projectileSpawns = new List<ProjectileSpawnPayload>(8);
        private readonly List<MuzzleFlashSpawnPayload> _flashSpawns = new List<MuzzleFlashSpawnPayload>(8);
        private readonly List<BeamSpawnPayload> _beamSpawns = new List<BeamSpawnPayload>(4);
        private readonly List<GroundWeaponSpawnPayload> _groundWeaponSpawns = new List<GroundWeaponSpawnPayload>(4);
        private readonly List<ShotEvent> _shotEvents = new List<ShotEvent>(8);

        /// <summary>Shot events recorded this frame. Read by listener systems; cleared in Playback.</summary>
        public IReadOnlyList<ShotEvent> ShotEvents => _shotEvents;

        // -----------------------------------------------------------------------
        // Public API
        // -----------------------------------------------------------------------

        public void AddComponent<T>(EntityId id, in T value) where T : struct, IEcsComponent
        {
            _ops.Add(new Op(OpKind.AddComponent, id, ComponentTypeId<T>.Value, BoxValue(value)));
        }

        public void RemoveComponent<T>(EntityId id) where T : struct, IEcsComponent
        {
            _ops.Add(new Op(OpKind.RemoveComponent, id, ComponentTypeId<T>.Value, null));
        }

        public void DestroyEntity(EntityId id)
        {
            _ops.Add(new Op(OpKind.Destroy, id, -1, null));
        }

        /// <summary>
        /// Record that <paramref name="shooter"/> fired a shot in <paramref name="direction"/>.
        /// Any system can listen via CommandBuffer.ShotEvents without any coupling to the caller.
        /// </summary>
        public void RecordShotEvent(EntityId shooter, UnityEngine.Vector2 direction)
        {
            _shotEvents.Add(new ShotEvent(shooter, direction));
        }

        public void EnqueueSpawnProjectile(in ProjectileSpawnPayload payload)
        {
            _projectileSpawns.Add(payload);
        }

        public void EnqueueSpawnMuzzleFlash(in MuzzleFlashSpawnPayload payload)
        {
            if (payload.Frames == null || payload.Frames.Length == 0) return;
            _flashSpawns.Add(payload);
        }

        public void EnqueueSpawnBeam(in BeamSpawnPayload payload)
        {
            _beamSpawns.Add(payload);
        }

        public void EnqueueSpawnGroundWeapon(in GroundWeaponSpawnPayload payload)
        {
            if (!payload.Entry.HasWeapon) return;
            _groundWeaponSpawns.Add(payload);
        }

        // -----------------------------------------------------------------------
        // Playback — called by EcsWorld at end of frame
        // -----------------------------------------------------------------------

        public void Playback(EcsWorld world)
        {
            foreach (Op op in _ops)
            {
                if (!world.IsAlive(op.Id)) continue;

                switch (op.Kind)
                {
                    case OpKind.AddComponent:
                    {
                        ComponentSignature current = world.GetSignature(op.Id);
                        ComponentSignature next = current.WithTypeId(op.TypeId);
                        if (next != current)
                        {
                            world.MigrateEntityImmediate(op.Id, next, op.TypeId, op.BoxedValue);
                        }

                        break;
                    }

                    case OpKind.RemoveComponent:
                    {
                        ComponentSignature current = world.GetSignature(op.Id);
                        ComponentSignature next = current.WithoutTypeId(op.TypeId);
                        if (next != current)
                        {
                            world.MigrateEntityImmediate(op.Id, next, -1, null);
                        }

                        break;
                    }

                    case OpKind.Destroy:
                        world.DestroyEntity(op.Id);
                        break;
                }
            }

            _ops.Clear();

            int count = _projectileSpawns.Count;
            for (int i = 0; i < count; i++)
            {
                ProjectileSpawnPayload payload = _projectileSpawns[i];
                SpawnProjectileImmediate(world, payload);
            }
            _projectileSpawns.Clear();

            int flashCount = _flashSpawns.Count;
            for (int i = 0; i < flashCount; i++)
            {
                SpawnMuzzleFlashImmediate(world, _flashSpawns[i]);
            }
            _flashSpawns.Clear();

            int beamCount = _beamSpawns.Count;
            for (int i = 0; i < beamCount; i++)
            {
                SpawnBeamImmediate(world, _beamSpawns[i]);
            }
            _beamSpawns.Clear();

            int groundCount = _groundWeaponSpawns.Count;
            for (int i = 0; i < groundCount; i++)
            {
                SpawnGroundWeaponImmediate(world, _groundWeaponSpawns[i]);
            }
            _groundWeaponSpawns.Clear();

            // Shot events are consumed by listener systems earlier in the same frame;
            // clear here so the list is empty at the start of the next frame.
            _shotEvents.Clear();
        }

        // -----------------------------------------------------------------------
        // Private helpers
        // -----------------------------------------------------------------------

        private static void SpawnProjectileImmediate(EcsWorld world, ProjectileSpawnPayload p)
        {
            // Unity-object creation lives here at the presentation boundary, not inside Core iteration.
            GameObject go = new GameObject("Projectile");
            go.transform.position = p.Position;
            go.transform.rotation = Quaternion.Euler(
                0f, 0f, Mathf.Atan2(p.Direction.y, p.Direction.x) * Mathf.Rad2Deg);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = p.ProjectileSprite != null ? p.ProjectileSprite : p.FallbackSprite;
            sr.sortingOrder = p.SortingOrder;
            go.transform.localScale = new Vector3(p.ProjectileScale, p.ProjectileScale, 1f);

            ComponentSignature sig = ComponentSignature.Empty
                .With<TransformComponent>()
                .With<ProjectileComponent>();

            EntityId id = world.CreateEntity(sig);
            world.GetComponent<TransformComponent>(id).Transform = go.transform;
            ref ProjectileComponent proj = ref world.GetComponent<ProjectileComponent>(id);
            proj.Direction = p.Direction;
            proj.Speed = p.Speed;
            proj.TimeLeft = p.Lifetime;
            proj.Damage = p.Damage;
            proj.Instigator = p.Instigator;
            proj.IsActive = true;
        }

        private static void SpawnMuzzleFlashImmediate(EcsWorld world, MuzzleFlashSpawnPayload p)
        {
            GameObject go = new GameObject("MuzzleFlash");
            go.transform.position = p.Position;
            go.transform.rotation = Quaternion.Euler(0f, 0f, p.RotationDeg);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = p.Frames[0];
            sr.sortingOrder = p.SortingOrder;
            float scale = p.Scale > 0f ? p.Scale : 1f;
            go.transform.localScale = new Vector3(scale, scale, 1f);

            ComponentSignature sig = ComponentSignature.Empty
                .With<TransformComponent>()
                .With<MuzzleFlashComponent>();

            EntityId id = world.CreateEntity(sig);
            world.GetComponent<TransformComponent>(id).Transform = go.transform;

            ref MuzzleFlashComponent flash = ref world.GetComponent<MuzzleFlashComponent>(id);
            flash.Renderer = sr;
            flash.Frames = p.Frames;
            flash.FrameDuration = p.FrameDuration;
            flash.TimeInCurrentFrame = 0f;
            flash.CurrentFrame = 0;
        }

        private const int BeamSegments = 12;

        private static void SpawnBeamImmediate(EcsWorld world, BeamSpawnPayload p)
        {
            GameObject go = new GameObject("LaserBeam");
            go.transform.position = Vector3.zero; // BeamAnimationSystem sets world-space positions

            Material beamMat = CreateBeamMaterial();

            // Core — narrow bright line.
            LineRenderer core = go.AddComponent<LineRenderer>();
            core.positionCount     = BeamSegments;
            core.useWorldSpace     = true;
            core.startWidth        = p.Width;
            core.endWidth          = p.Width * 0.4f;
            core.startColor        = Color.white;
            core.endColor          = p.Color;
            core.sortingLayerName  = "Default";
            core.sortingOrder      = p.SortingOrder;
            core.material          = beamMat;
            core.numCapVertices    = 4;

            // Glow — wide transparent halo on a child object.
            GameObject glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(go.transform, false);
            LineRenderer glow = glowGo.AddComponent<LineRenderer>();
            glow.positionCount    = BeamSegments;
            glow.useWorldSpace    = true;
            glow.startWidth       = p.Width * 4f;
            glow.endWidth         = p.Width * 1.5f;
            Color glowColor       = p.Color;
            glowColor.a           = 0.25f;
            glow.startColor       = glowColor;
            Color glowEnd         = glowColor;
            glowEnd.a             = 0.05f;
            glow.endColor         = glowEnd;
            glow.sortingLayerName = "Default";
            glow.sortingOrder     = p.SortingOrder - 1;
            glow.material         = beamMat;
            glow.numCapVertices   = 4;

            ComponentSignature sig = ComponentSignature.Empty
                .With<TransformComponent>()
                .With<BeamComponent>();

            EntityId id = world.CreateEntity(sig);
            world.GetComponent<TransformComponent>(id).Transform = go.transform;

            ref BeamComponent beam    = ref world.GetComponent<BeamComponent>(id);
            beam.Core            = core;
            beam.Glow            = glow;
            beam.Origin          = p.Origin;
            beam.Direction       = p.Direction.normalized;
            beam.Length          = p.Length;
            beam.TimeLeft        = p.Duration;
            beam.InitialDuration = p.Duration;
            beam.Width           = p.Width;
            beam.Color           = p.Color;
            beam.Age             = 0f;
            beam.NoiseSeed       = (float)(id.GetHashCode() & 0xFFFF) / 0xFFFF * 100f;
            beam.SegmentCount    = BeamSegments;
        }

        /// <summary>
        /// Creates a material that correctly renders LineRenderer vertex colors.
        /// Uses Sprites/Default (works in Built-in and URP as a fallback).
        /// The shader must support vertex color interpolation — avoid Unlit/Color.
        /// </summary>
        private static void SpawnGroundWeaponImmediate(EcsWorld world, GroundWeaponSpawnPayload p)
        {
            GameObject go = new GameObject("DroppedWeapon");
            go.transform.position = p.Position;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = p.Entry.WeaponSprite;
            float scale = p.Entry.WeaponSpriteScale > 0f ? p.Entry.WeaponSpriteScale : 1f;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            sr.sortingOrder = p.Entry.WeaponSortingOrder;

            ComponentSignature sig = ComponentSignature.Empty
                .With<GroundWeaponTagComponent>()
                .With<TransformComponent>()
                .With<WeaponComponent>()
                .With<GroundWeaponHintViewComponent>();

            EntityId id = world.CreateEntity(sig);
            world.GetComponent<TransformComponent>(id).Transform = go.transform;

            ref WeaponComponent weapon = ref world.GetComponent<WeaponComponent>(id);
            weapon = WeaponInventoryUtility.ToWeaponComponent(in p.Entry);

            world.GetComponent<GroundWeaponHintViewComponent>(id) =
                GroundWeaponHintFactory.Create(go.transform);
        }

        private static Material CreateBeamMaterial()
        {
            // Sprites/Default properly reads vertexColor on LineRenderer in Built-in RP.
            // In URP this falls back to Universal/Unlit with vertex colors enabled.
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            var mat = new Material(shader);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // additive — laser-like glow
            mat.renderQueue = 3000;
            return mat;
        }

        private static object BoxValue<T>(in T value) where T : struct => value;
    }

    /// <summary>Plain data payload for deferred projectile spawning.</summary>
    public struct ProjectileSpawnPayload
    {
        public Vector3 Position;
        public Vector2 Direction;
        public float Speed;
        public float Lifetime;
        public float Damage;
        public EntityId Instigator;
        public Sprite ProjectileSprite;
        public Sprite FallbackSprite;
        public float ProjectileScale;
        public int SortingOrder;
    }

    /// <summary>Plain data payload for deferred muzzle-flash spawning.</summary>
    public struct MuzzleFlashSpawnPayload
    {
        public Vector3 Position;
        public float RotationDeg;
        public Sprite[] Frames;
        public float FrameDuration;
        public float Scale;
        public int SortingOrder;
    }

    public struct GroundWeaponSpawnPayload
    {
        public Vector3 Position;
        public WeaponSlotEntry Entry;
    }

    /// <summary>Plain data payload for deferred laser-beam spawning.</summary>
    public struct BeamSpawnPayload
    {
        public Vector2 Origin;
        public Vector2 Direction;
        public float   Duration;
        public float   Length;
        public float   Width;
        public Color   Color;
        public int     SortingOrder;
    }
}
