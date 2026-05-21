using UnityEngine;

namespace Chronocaust.Ecs
{
    /// <summary>
    /// Scene authoring for enemy entities converted in EcsCombatBootstrap.
    /// </summary>
    public sealed class EnemyAuthoring : MonoBehaviour
    {
        [Header("Combat")]
        [Min(1f)] public float MaxHealth = 40f;
        [Min(0.05f)] public float AttackRange = 1.2f;
        [Min(0.1f)] public float UnarmedDamage = 5f;
        [Tooltip("If set, enemy damage/cooldown are taken from this weapon.")]
        public WeaponDefinition StartingWeapon;

        [Header("Movement")]
        [Min(0.01f)] public float MoveSpeed = 3.5f;
        [Min(0f)] public float StopDistance = 0.85f;
    }
}
