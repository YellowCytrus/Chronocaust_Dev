using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;

namespace Chronocaust.Ecs.Systems
{
    public sealed class ProjectileLifetimeSystem : IEcsUpdateSystem
    {
        private EcsQuery<ProjectileComponent, TransformComponent> _query;

        public void Update(EcsWorld world, float deltaTime)
        {
            _query ??= world.CreateQuery<ProjectileComponent, TransformComponent>();

            _query.ForEach((EntityId id,
                ref ProjectileComponent projectile,
                ref TransformComponent _) =>
            {
                projectile.TimeLeft -= deltaTime;
                if (projectile.TimeLeft <= 0f)
                {
                    world.DestroyEntity(id);
                }
            });
        }
    }
}
