using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;

namespace Chronocaust.Ecs.Systems
{
    public sealed class ProjectileLifetimeSystem : IEcsUpdateSystem
    {
        public void Update(EcsWorld world, float deltaTime)
        {
            foreach (EcsEntity entity in world.Entities)
            {
                if (!entity.TryGet(out ProjectileComponent projectile))
                {
                    continue;
                }

                projectile.TimeLeft -= deltaTime;
                if (projectile.TimeLeft <= 0f)
                {
                    world.DestroyEntity(entity);
                }
            }
        }
    }
}
