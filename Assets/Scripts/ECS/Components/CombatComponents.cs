using Chronocaust.Ecs.Core;
using UnityEngine;
using Chronocaust.Ecs;

namespace Chronocaust.Ecs.Components
{
    public struct PlayerTagComponent : IEcsComponent { }

    public struct TransformComponent : IEcsComponent
    {
        public Transform Transform;
    }

    public struct InputStateComponent : IEcsComponent
    {
        public bool FirePressed;
        public bool InteractPressed;
        public Vector3 MouseWorldPosition;
        public Vector2 MoveInput;
    }

    public struct AimComponent : IEcsComponent
    {
        public Vector2 Direction;
    }

    /// <summary>
    /// Data stored on a ground pickup entity. Never placed directly on a character.
    /// </summary>
    public struct WeaponComponent : IEcsComponent
    {
        public Sprite WeaponSprite;
        public Sprite ProjectileSprite;
        public float WeaponSpriteScale;
        public float ProjectileSpriteScale;
        public int WeaponSortingOrder;
        public int ProjectileSortingOrder;
        public float FireRate;
        public float ProjectileSpeed;
        public float ProjectileLifetime;
        public Vector2 MuzzleOffset;
        public float WeaponVisualBaseRotationDeg;
        public bool WeaponVisualMirrorX;
        public bool WeaponVisualMirrorY;
        public float MovementSpeedMultiplier;
        public Sprite[] ShootEffectFrames;
        public float ShootEffectFrameDuration;
        public float ShootEffectScale;
        public Vector2 ShootEffectMuzzleOffset;
        /// <summary>Determines which type-specific tag+data components WeaponPickupSystem adds on pickup.</summary>
        public WeaponKind Kind;
        // Type-specific data — only the fields matching Kind are used.
        public int   PelletCount;
        public float SpreadAngle;
        public float BeamDuration;
        public float BeamWidth;
        public Color BeamColor;
        public float MeleeRange;
        public float MeleeArcAngle;
        public MeleeMotionType MeleeMotionType;
        public float MeleeStartupDuration;
        public float MeleeActiveDuration;
        public float MeleeRecoveryDuration;
        public float MeleeHitWindowStartT;
        public float MeleeHitWindowEndT;
        public float MeleeAnticipationPull;
        public float MeleeThrustDistance;
        public float MeleeThrustHitRadius;
        public float MeleeThrustVisualTiltMaxDeg;
        public float MeleeSlamWindupDeg;
        public float MeleeSlamDownDeg;
        public float MeleeSlamWindupOffsetY;
        public float MeleeSlamStrikeDepth;
        public bool MeleeSlamUseFixedAimDir;
        public Vector2 MeleeSlamPoseOffsetRight;
        public float MeleeSlamPoseRotRight;
        public Vector2 MeleeSlamPoseOffsetLeft;
        public float MeleeSlamPoseRotLeft;
        public Vector2 MeleeSlamShootEffectOffsetRight;
        public Vector2 MeleeSlamShootEffectOffsetLeft;
        /// <summary>Fixed slam: hit center lateral offset from player (world X, meters).</summary>
        public float MeleeSlamHitSideOffset;
        public float MeleeSpinTurns;
        /// <summary>If true: no SpriteRenderer.flipY — left/right via Z rotation only (aim-space semantics).</summary>
        public bool MeleeViewSuppressFlipY;
        /// <summary>Idle melee visual aim smoothing (1/s) on pickup; 0 = instant.</summary>
        public float MeleeIdleVisualAimSmoothHz;
        public float RecoilStrength;
        public float RecoilDecayRate;
    }

    public struct GroundWeaponTagComponent : IEcsComponent { }

    /// <summary>
    /// The weapon currently held by this entity (player, enemy, etc.).
    /// All fields are default/zero when no weapon is equipped.
    /// Type-specific behaviour is expressed by the presence of ShotgunTagComponent,
    /// LaserTagComponent, or MeleeTagComponent — not by any field on this struct.
    /// </summary>
    public struct EquippedWeaponComponent : IEcsComponent
    {
        public Sprite WeaponSprite;
        public Sprite ProjectileSprite;
        public float WeaponSpriteScale;
        public float ProjectileSpriteScale;
        public int WeaponSortingOrder;
        public int ProjectileSortingOrder;
        public float FireRate;
        public float ProjectileSpeed;
        public float ProjectileLifetime;
        public Vector2 MuzzleOffset;
        /// <summary>Added to aim angle + melee pose in WeaponViewSystem (sprite art offset in degrees).</summary>
        public float WeaponVisualBaseRotationDeg;
        public bool WeaponVisualMirrorX;
        public bool WeaponVisualMirrorY;
        /// <summary>Multiplied against MovementComponent.BaseSpeed each frame. Default 1 = no change.</summary>
        public float MovementSpeedMultiplier;
        public Sprite[] ShootEffectFrames;
        public float ShootEffectFrameDuration;
        public float ShootEffectScale;
        public Vector2 ShootEffectMuzzleOffset;
        public float RecoilStrength;
        public float RecoilDecayRate;

        public bool HasWeapon => WeaponSprite != null && FireRate > 0f;

        public static EquippedWeaponComponent From(in WeaponComponent source) => new EquippedWeaponComponent
        {
            WeaponSprite = source.WeaponSprite,
            ProjectileSprite = source.ProjectileSprite,
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
        };
    }

    /// <summary>Relative cooldown: decremented by deltaTime. No dependency on Time.time.</summary>
    public struct WeaponCooldownComponent : IEcsComponent
    {
        public float CooldownRemaining;
    }

    public struct WeaponViewComponent : IEcsComponent
    {
        public Transform Transform;
        public SpriteRenderer Renderer;
    }

    public struct ProjectileComponent : IEcsComponent
    {
        public Vector2 Direction;
        public float Speed;
        public float TimeLeft;
    }

    public struct RigidbodyComponent : IEcsComponent
    {
        public Rigidbody2D Rigidbody;
    }

    public struct MovementComponent : IEcsComponent
    {
        /// <summary>Base speed of the character, set in PlayerAuthoring. Never modified at runtime.</summary>
        public float BaseSpeed;
        public bool UseIsometricAxes;
        public Vector2 IsometricRightAxis;
        public Vector2 IsometricUpAxis;
        /// <summary>Written by PlayerMovementSystem; read by CharacterAnimationSystem (rendering layer).</summary>
        public Vector2 LastDirection;
    }

    public struct CharacterRenderComponent : IEcsComponent
    {
        public IsometricCharacterRenderer Renderer;
    }

    /// <summary>
    /// Accumulates recoil impulse on each shot. Decayed exponentially by RecoilDecaySystem.
    /// Added to rigidbody velocity in PlayerMovementSystem.
    /// </summary>
    public struct RecoilComponent : IEcsComponent
    {
        /// <summary>Current recoil velocity in world space (units/s). Decays toward zero.</summary>
        public Vector2 CurrentVelocity;
    }

    /// <summary>
    /// Drives a short-lived muzzle-flash animation. Entity is destroyed when all frames have played.
    /// </summary>
    public struct MuzzleFlashComponent : IEcsComponent
    {
        public SpriteRenderer Renderer;
        public Sprite[] Frames;
        public float FrameDuration;
        public float TimeInCurrentFrame;
        public int CurrentFrame;
    }

    // -----------------------------------------------------------------------
    // Weapon type tags — at most one is present on an armed entity at a time.
    // Absence of all tags = default (pistol/auto) behaviour.
    // WeaponShootSystem excludes all three; each specialised system includes its own.
    // -----------------------------------------------------------------------

    public struct ShotgunTagComponent : IEcsComponent { }
    public struct LaserTagComponent   : IEcsComponent { }
    public struct MeleeTagComponent   : IEcsComponent { }

    /// <summary>Shotgun-specific parameters. Paired with ShotgunTagComponent.</summary>
    public struct ShotgunDataComponent : IEcsComponent
    {
        public int   PelletCount;
        public float SpreadAngle;
    }

    /// <summary>Laser-specific parameters. Paired with LaserTagComponent.</summary>
    public struct LaserDataComponent : IEcsComponent
    {
        public float BeamDuration;
        public float BeamWidth;
        public Color BeamColor;
    }

    /// <summary>Melee-specific parameters + per-attack runtime. Paired with MeleeTagComponent.</summary>
    public struct MeleeDataComponent : IEcsComponent
    {
        public MeleeMotionType MotionType;
        public float Range;
        public float ArcAngle;

        public float StartupDuration;
        public float ActiveDuration;
        public float RecoveryDuration;

        /// <summary>Hit checks use normalized time over the whole swing [0,1].</summary>
        public float HitWindowStartT;
        public float HitWindowEndT;

        public float AnticipationPull;
        public float ThrustDistance;
        public float ThrustHitRadius;
        /// <summary>Extra Z rotation on weapon sprite during thrust only; degrees, visual only.</summary>
        public float ThrustVisualTiltMaxDeg;

        public float SlamWindupDeg;
        public float SlamDownDeg;
        public float SlamWindupOffsetY;
        public float SlamStrikeDepth;
        public bool SlamUseFixedAimDir;
        public Vector2 SlamPoseOffsetRight;
        public float SlamPoseRotRight;
        public Vector2 SlamPoseOffsetLeft;
        public float SlamPoseRotLeft;
        public Vector2 SlamShootEffectOffsetRight;
        public Vector2 SlamShootEffectOffsetLeft;
        public float SlamHitSideOffset;

        public float SpinTurns;

        public bool MeleeViewSuppressFlipY;
        /// <summary>Idle visual aim smoothing rate (1/s), from weapon authoring. 0 = instant follow.</summary>
        public float MeleeIdleVisualAimSmoothHz;
        /// <summary>WeaponViewSystem: smoothed aim angle (deg) for idle; chases live Aim every frame.</summary>
        public float IdleVisualSmoothedAimDeg;

        public bool AttackActive;
        public MeleeAttackPhase AttackPhase;
        public float AttackElapsed;
        public Vector2 AttackAimDir;
        /// <summary>Side-based attacks (fixed slam): cursor left of player at attack start.</summary>
        public bool AttackFacingLeft;
        public byte HitCount;
        public int HitId0;
        public int HitId1;
        public int HitId2;
        public int HitId3;
        public int HitId4;
        public int HitId5;
        public int HitId6;
        public int HitId7;
        public int HitId8;
        public int HitId9;
        public int HitId10;
        public int HitId11;
        public int HitId12;
        public int HitId13;
        public int HitId14;
        public int HitId15;
    }

    /// <summary>
    /// Lives on the beam entity (not the player). Drives LineRenderer and lifetime.
    /// BeamAnimationSystem reads Age, NoiseSeed, SegmentCount and writes per-frame positions.
    /// </summary>
    public struct BeamComponent : IEcsComponent
    {
        public LineRenderer Core;       // thin bright inner line
        public LineRenderer Glow;       // wide transparent outer halo
        public Vector2 Origin;
        public Vector2 Direction;
        public float   Length;
        public float   TimeLeft;
        public float   InitialDuration; // for life-ratio fade
        public float   Width;
        public Color   Color;
        public float   Age;             // incremented by BeamAnimationSystem (rendering layer)
        public float   NoiseSeed;       // unique per-beam for variation
        public int     SegmentCount;    // number of arc points (set at spawn)
    }
}
