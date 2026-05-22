using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;

namespace Chronocaust.Ecs.Systems
{
    /// <summary>
    /// Removes dead enemies from the world.
    /// </summary>
    public sealed class EnemyDeathSystem : IEcsUpdateSystem
    {
        private EcsQuery<EnemyTagComponent, HealthComponent> _query;

        public void Update(EcsWorld world, float deltaTime)
        {
            _query ??= world.CreateQuery<EnemyTagComponent, HealthComponent>();

            _query.ForEach((EntityId id,
                ref EnemyTagComponent _enemyTag,
                ref HealthComponent health) =>
            {
                if (health.Current <= 0f)
                {
                    world.CommandBuffer.DestroyEntity(id);
                }
            });
        }
    }
}
