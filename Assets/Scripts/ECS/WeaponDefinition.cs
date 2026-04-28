using UnityEngine;

namespace Chronocaust.Ecs
{
    [CreateAssetMenu(
        fileName = "WeaponDefinition",
        menuName = "Chronocaust/ECS/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [Header("Visuals")]
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
    }
}
