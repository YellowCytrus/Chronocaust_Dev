using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;

namespace Chronocaust.Ecs.Systems
{
    /// <summary>
    /// Decrements BeamComponent.TimeLeft and destroys the beam entity when it expires.
    /// Does NOT call world.DestroyEntity directly inside ForEach — uses CommandBuffer.
    /// </summary>
    public sealed class BeamLifetimeSystem : IEcsUpdateSystem
    {
        private EcsQuery<BeamComponent, TransformComponent> _query;

        public void Update(EcsWorld world, float deltaTime)
        {
            _query ??= world.CreateQuery<BeamComponent, TransformComponent>();

            _query.ForEach((EntityId id,
                ref BeamComponent beam,
                ref TransformComponent _) =>
            {
                beam.TimeLeft -= deltaTime;

                if (beam.TimeLeft <= 0f)
                {
                    world.CommandBuffer.DestroyEntity(id);
                }
            });
        }
    }
}
