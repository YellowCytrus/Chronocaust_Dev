using Chronocaust.Ecs;
using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using UnityEngine;

namespace Chronocaust.Ecs.Systems
{
    /// <summary>
    /// FixedUpdate: projectile sweep hits before movement. Deactivates projectile immediately on hit.
    /// </summary>
    public sealed class ProjectileHitSystem : IEcsFixedUpdateSystem
    {
        private const float HitRadius = 0.12f;

        private readonly PhysicsEntityRegistry _registry;
        private readonly int _damageableLayerMask;

        private EcsQuery<ProjectileComponent, TransformComponent> _query;

        public ProjectileHitSystem(PhysicsEntityRegistry registry, LayerMask damageableLayers)
        {
            _registry = registry;
            _damageableLayerMask = damageableLayers.value;
        }

        public void FixedUpdate(EcsWorld world, float deltaTime)
        {
            _query ??= world.CreateQuery<ProjectileComponent, TransformComponent>();

            _query.ForEach((EntityId projectileId,
                ref ProjectileComponent projectile,
                ref TransformComponent transform) =>
            {
                if (!projectile.IsActive || transform.Transform == null)
                {
                    return;
                }

                Vector2 origin = transform.Transform.position;
                Vector2 step = projectile.Direction * projectile.Speed * deltaTime;
                float distance = step.magnitude;
                if (distance < 0.0001f)
                {
                    return;
                }

                RaycastHit2D hit = Physics2D.CircleCast(
                    origin,
                    HitRadius,
                    step.normalized,
                    distance,
                    _damageableLayerMask);

                if (hit.collider == null)
                {
                    return;
                }

                if (!_registry.TryResolve(hit.collider, out EntityId target))
                {
                    return;
                }

                if (target == projectile.Instigator)
                {
                    return;
                }

                float damage = projectile.Damage > 0f ? projectile.Damage : 1f;
                if (!CombatDamage.Apply(world, projectile.Instigator, target, damage))
                {
                    return;
                }

                projectile.IsActive = false;
                projectile.TimeLeft = 0f;
                world.CommandBuffer.DestroyEntity(projectileId);
            });
        }
    }
}
