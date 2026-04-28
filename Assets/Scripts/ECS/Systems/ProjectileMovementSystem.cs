using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;

namespace Chronocaust.Ecs.Systems
{
    public sealed class ProjectileMovementSystem : IEcsFixedUpdateSystem
    {
        public void FixedUpdate(EcsWorld world, float deltaTime)
        {
            foreach (EcsEntity entity in world.Entities)
            {
                if (!entity.TryGet(out ProjectileComponent projectile) ||
                    !entity.TryGet(out TransformComponent transformComponent) ||
                    transformComponent.Transform == null)
                {
                    continue;
                }

                transformComponent.Transform.position += (UnityEngine.Vector3)(projectile.Direction * projectile.Speed * deltaTime);
            }
        }
    }
}
