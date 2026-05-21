using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using UnityEngine;

namespace Chronocaust.Ecs.Systems
{
    /// <summary>
    /// FixedUpdate simulation: enemies chase player and keep aim toward the player.
    /// </summary>
    public sealed class EnemyChaseSystem : IEcsFixedUpdateSystem
    {
        private EcsQuery<PlayerTagComponent, TransformComponent> _playerQuery;
        private EcsQuery<EnemyTagComponent, TransformComponent, AimComponent, MovementComponent,
            EnemyChaseComponent, EquippedWeaponComponent, RigidbodyComponent> _enemyQuery;

        public void FixedUpdate(EcsWorld world, float deltaTime)
        {
            _playerQuery ??= world.CreateQuery<PlayerTagComponent, TransformComponent>();
            _enemyQuery ??= world.CreateQuery<EnemyTagComponent, TransformComponent, AimComponent, MovementComponent,
                EnemyChaseComponent, EquippedWeaponComponent, RigidbodyComponent>();

            Vector2 playerPos = Vector2.zero;
            bool hasPlayer = false;
            _playerQuery.ForEach((EntityId _id,
                ref PlayerTagComponent _tag,
                ref TransformComponent playerTransform) =>
            {
                if (playerTransform.Transform == null) return;
                playerPos = playerTransform.Transform.position;
                hasPlayer = true;
            });

            if (!hasPlayer) return;

            _enemyQuery.ForEach((EntityId _id,
                ref EnemyTagComponent _enemyTag,
                ref TransformComponent transform,
                ref AimComponent aim,
                ref MovementComponent movement,
                ref EnemyChaseComponent chase,
                ref EquippedWeaponComponent equipped,
                ref RigidbodyComponent rb) =>
            {
                if (transform.Transform == null || rb.Rigidbody == null) return;

                Vector2 enemyPos = transform.Transform.position;
                Vector2 toPlayer = playerPos - enemyPos;
                float sqrDistance = toPlayer.sqrMagnitude;
                if (sqrDistance > 0.0001f)
                {
                    aim.Direction = toPlayer.normalized;
                }

                float stopDistance = chase.StopDistance > 0f ? chase.StopDistance : 0.8f;
                if (sqrDistance <= stopDistance * stopDistance)
                {
                    rb.Rigidbody.linearVelocity = Vector2.zero;
                    movement.LastDirection = Vector2.zero;
                    return;
                }

                Vector2 direction = toPlayer.normalized;
                float speed = chase.MoveSpeed > 0f ? chase.MoveSpeed : 3.5f;
                float multiplier = equipped.HasWeapon ? equipped.MovementSpeedMultiplier : 1f;
                rb.Rigidbody.linearVelocity = direction * speed * multiplier;
                movement.LastDirection = direction;
            });
        }
    }
}
