using Chronocaust.Ecs.Core;
using UnityEngine;
using Chronocaust.Ecs;

namespace Chronocaust.Ecs.Components
{
    public struct PlayerTagComponent : IEcsComponent { }

    /// <summary>Hit points. Simulation systems write Current; HUD reads both fields.</summary>
    public struct HealthComponent : IEcsComponent
    {
        public float Current;
        public float Max;

        public float Ratio => Max > 0f ? Mathf.Clamp01(Current / Max) : 0f;
        public bool IsAlive => Current > 0f;
    }

    /// <summary>Scene-authored UI references. Written only by PlayerHudSystem (rendering layer).</summary>
    public struct PlayerHudViewComponent : IEcsComponent
    {
        public UnityEngine.UI.Text HealthLabelText;
        public UnityEngine.UI.Text HealthValueText;
        public UnityEngine.UI.Image HealthFill;
        public UnityEngine.UI.Image WeaponIcon;
        public UnityEngine.UI.Image CooldownOverlay;
        public UnityEngine.UI.Image CooldownBarFill;
        public UnityEngine.UI.Image Slot0Icon;
        public UnityEngine.UI.Image Slot1Icon;
        public UnityEngine.UI.Image Slot0Frame;
        public UnityEngine.UI.Image Slot1Frame;
        public UnityEngine.UI.Text WeaponNameText;
        public UnityEngine.UI.Text WeaponStatsText;
        public UnityEngine.UI.Text HintText;
        public GameObject HealthPanelRoot;
        public GameObject WeaponPanelRoot;
    }

    /// <summary>Serialized weapon in one of the player's two inventory slots.</summary>
    public struct WeaponSlotEntry
    {
        public string DisplayName;
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
        public float MovementSpeedMultiplier;
        public Sprite[] ShootEffectFrames;
        public float ShootEffectFrameDuration;
        public float ShootEffectScale;
        public Vector2 ShootEffectMuzzleOffset;
        public float RecoilStrength;
        public float RecoilDecayRate;
        public WeaponKind Kind;
        public int PelletCount;
        public float SpreadAngle;
        public float BeamDuration;
        public float BeamWidth;
        public Color BeamColor;
        public float MeleeRange;
        public float MeleeArcAngle;
        public float CooldownRemaining;

        public bool HasWeapon => WeaponSprite != null && FireRate > 0f;

        public static WeaponSlotEntry Empty => default;
    }

    /// <summary>Two weapon slots; <see cref="EquippedWeaponComponent"/> mirrors the active slot for combat systems.</summary>
    public struct WeaponLoadoutComponent : IEcsComponent
    {
        public const int SlotCount = 2;

        public WeaponSlotEntry Slot0;
        public WeaponSlotEntry Slot1;
        /// <summary>0 or 1 — which slot is currently wielded.</summary>
        public int ActiveIndex;
    }

    /// <summary>World-space pickup prompt above a ground weapon.</summary>
    public struct GroundWeaponHintViewComponent : IEcsComponent
    {
        public Transform Root;
        public UnityEngine.UI.Text Label;
        public CanvasGroup Group;
    }

    public struct TransformComponent : IEcsComponent
    {
        public Transform Transform;
    }

    public struct InputStateComponent : IEcsComponent
    {
        public bool FirePressed;
        public bool InteractPressed;
        public bool DropPressed;
        public bool SelectSlot0Pressed;
        public bool SelectSlot1Pressed;
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
        public string DisplayName;
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
        public string DisplayName;
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
            DisplayName = source.DisplayName,
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

    /// <summary>Melee-specific parameters. Paired with MeleeTagComponent.</summary>
    public struct MeleeDataComponent : IEcsComponent
    {
        public float Range;
        public float ArcAngle;
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
