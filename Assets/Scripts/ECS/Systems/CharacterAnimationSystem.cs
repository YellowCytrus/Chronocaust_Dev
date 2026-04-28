using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;

namespace Chronocaust.Ecs.Systems
{
    /// <summary>
    /// Rendering layer: reads LastDirection (written by PlayerMovementSystem in FixedUpdate) and
    /// drives IsometricCharacterRenderer. Strictly separated from simulation.
    /// </summary>
    public sealed class CharacterAnimationSystem : IEcsUpdateSystem
    {
        private EcsQuery<MovementComponent, CharacterRenderComponent> _query;

        public void Update(EcsWorld world, float deltaTime)
        {
            _query ??= world.CreateQuery<MovementComponent, CharacterRenderComponent>();

            _query.ForEach((EntityId id,
                ref MovementComponent movement,
                ref CharacterRenderComponent render) =>
            {
                if (render.Renderer == null) return;
                render.Renderer.SetDirection(movement.LastDirection);
            });
        }
    }
}
