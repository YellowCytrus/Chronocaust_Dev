using UnityEngine;

namespace Chronocaust.Ecs
{
    [CreateAssetMenu(
        fileName = "WeaponDefinition",
        menuName = "Chronocaust/ECS/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [Header("Visuals")]
        [Tooltip("Shown on the combat HUD. Falls back to weapon type name when empty.")]
        public string DisplayName;
        public Sprite WeaponSprite;
        public Sprite ProjectileSprite;
        [Tooltip("Uniform scale applied to the weapon sprite in the WeaponView child object.")]
        [Min(0.01f)] public float WeaponSpriteScale = 1f;
        [Tooltip("Uniform scale applied to the projectile sprite.")]
        [Min(0.01f)] public float ProjectileSpriteScale = 1f;
        public int WeaponSortingOrder = 100;
        public int ProjectileSortingOrder = 90;

        [Header("Stats")]
        [Min(0.1f)] public float FireRate = 6f;
        [Min(0.1f)] public float ProjectileSpeed = 10f;
        [Min(0.1f)] public float ProjectileLifetime = 2f;
        public Vector2 MuzzleOffset = new Vector2(0.45f, 0f);

        [Header("Character Modifiers")]
        [Tooltip("Multiplies the bearer's base movement speed. 1 = no change, 0.8 = 20% slower.")]
        [Min(0.01f)] public float MovementSpeedMultiplier = 1f;

        [Header("Recoil")]
        [Tooltip("Kick strength (units/s) applied opposite to the shoot direction on each shot. 0 = no recoil.")]
        [Min(0f)] public float RecoilStrength = 1.5f;
        [Tooltip("Exponential decay rate. Higher = faster recovery. 8 ≈ dissipates in ~0.3 s.")]
        [Min(0.1f)] public float RecoilDecayRate = 8f;

        [Header("Shoot Effect")]
        [Tooltip("Sprite sheet for the muzzle flash (288x48, 6 frames). Drag one of the 4 Shoot_effects PNGs here.")]
        public Sprite[] ShootEffectFrames;
        [Tooltip("Duration of each muzzle flash frame in seconds.")]
        [Min(0.01f)] public float ShootEffectFrameDuration = 0.05f;
        [Tooltip("Uniform scale of the muzzle flash sprite.")]
        [Min(0.01f)] public float ShootEffectScale = 1f;
        [Tooltip("Local offset of the flash relative to the muzzle, in weapon-space (X = forward, Y = up).")]
        public Vector2 ShootEffectMuzzleOffset = Vector2.zero;

        [Header("Weapon Type")]
        [Tooltip("Determines which shoot system handles this weapon.")]
        public WeaponKind Kind = WeaponKind.Default;

        [Header("Shotgun")]
        [Tooltip("Number of pellets fired per shot.")]
        [Min(1)] public int PelletCount = 8;
        [Tooltip("Total spread cone angle in degrees.")]
        [Min(0f)] public float SpreadAngle = 30f;

        [Header("Laser")]
        [Tooltip("How long the beam stays visible (seconds).")]
        [Min(0.01f)] public float BeamDuration = 0.3f;
        [Tooltip("Visual width of the beam line.")]
        [Min(0.01f)] public float BeamWidth = 0.1f;
        public Color BeamColor = Color.red;

        [Header("Melee")]
        [Tooltip("Attack reach radius.")]
        [Min(0.1f)] public float MeleeRange = 1.5f;
        [Tooltip("Arc width of the swing in degrees.")]
        [Min(1f)] public float MeleeArcAngle = 90f;
    }
}
