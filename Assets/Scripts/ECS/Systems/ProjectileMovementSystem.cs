using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using UnityEngine;

namespace Chronocaust.Ecs.Systems
{
    public sealed class ProjectileMovementSystem : IEcsFixedUpdateSystem
    {
        private EcsQuery<ProjectileComponent, TransformComponent> _query;

        public void FixedUpdate(EcsWorld world, float deltaTime)
        {
            _query ??= world.CreateQuery<ProjectileComponent, TransformComponent>();

            _query.ForEach((EntityId id,
                ref ProjectileComponent projectile,
                ref TransformComponent transform) =>
            {
                if (transform.Transform == null) return;
                transform.Transform.position += (Vector3)(projectile.Direction * projectile.Speed * deltaTime);
            });
        }
    }
}
