using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using UnityEngine;

namespace Chronocaust.Ecs.Systems
{
    /// <summary>
    /// FixedUpdate simulation: enemies damage the player while in attack range.
    /// Runs in FixedUpdate together with chase/movement so distance checks use the same positions.
    /// </summary>
    public sealed class EnemyAttackSystem : IEcsFixedUpdateSystem
    {
        private const float RangeSlack = 0.15f;

        private EcsQuery<PlayerTagComponent, TransformComponent, HealthComponent> _playerQuery;
        private EcsQuery<EnemyTagComponent, TransformComponent, EquippedWeaponComponent,
            WeaponCooldownComponent, EnemyAttackComponent, EnemyChaseComponent> _enemyQuery;

        public void FixedUpdate(EcsWorld world, float deltaTime)
        {
            _playerQuery ??= world.CreateQuery<PlayerTagComponent, TransformComponent, HealthComponent>();
            _enemyQuery ??= world.CreateQuery<EnemyTagComponent, TransformComponent, EquippedWeaponComponent,
                WeaponCooldownComponent, EnemyAttackComponent, EnemyChaseComponent>();

            EntityId playerId = default;
            Vector2 playerPos = Vector2.zero;
            bool hasPlayer = false;
            _playerQuery.ForEach((EntityId id,
                ref PlayerTagComponent _tag,
                ref TransformComponent playerTransform,
                ref HealthComponent _health) =>
            {
                if (playerTransform.Transform == null) return;
                playerId = id;
                playerPos = playerTransform.Transform.position;
                hasPlayer = true;
            });

            if (!hasPlayer || !world.IsAlive(playerId)) return;

            _enemyQuery.ForEach((EntityId enemyId,
                ref EnemyTagComponent _enemyTag,
                ref TransformComponent enemyTransform,
                ref EquippedWeaponComponent equipped,
                ref WeaponCooldownComponent cooldown,
                ref EnemyAttackComponent attack,
                ref EnemyChaseComponent chase) =>
            {
                if (enemyTransform.Transform == null) return;

                if (cooldown.CooldownRemaining > 0f)
                {
                    cooldown.CooldownRemaining -= deltaTime;
                }

                Vector2 enemyPos = enemyTransform.Transform.position;
                float stopDistance = chase.StopDistance > 0f ? chase.StopDistance : 0.85f;
                float attackRange = attack.AttackRange > 0f ? attack.AttackRange : 1.2f;
                float range = Mathf.Max(attackRange, stopDistance) + RangeSlack;
                float sqrDistance = (playerPos - enemyPos).sqrMagnitude;
                if (sqrDistance > range * range || cooldown.CooldownRemaining > 0f)
                {
                    return;
                }

                if (!world.IsAlive(playerId)) return;

                float damage = equipped.HasWeapon && equipped.Damage > 0f
                    ? equipped.Damage
                    : (attack.UnarmedDamage > 0f ? attack.UnarmedDamage : 1f);

                ref HealthComponent playerHealth = ref world.GetComponent<HealthComponent>(playerId);
                playerHealth.Current = Mathf.Max(0f, playerHealth.Current - damage);

                float fireRate = equipped.HasWeapon && equipped.FireRate > 0f ? equipped.FireRate : 1f;
                cooldown.CooldownRemaining = 1f / fireRate;
            });
        }
    }
}
