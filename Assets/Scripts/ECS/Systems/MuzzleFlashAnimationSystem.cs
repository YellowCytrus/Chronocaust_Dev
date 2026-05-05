using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;

namespace Chronocaust.Ecs.Systems
{
    /// <summary>
    /// Advances the muzzle-flash sprite animation frame by frame.
    /// Destroys the entity (and its linked GameObject) when all frames have played.
    /// Pure system — no hidden state, reads DeltaTime as data.
    /// </summary>
    public sealed class MuzzleFlashAnimationSystem : IEcsUpdateSystem
    {
        private EcsQuery<MuzzleFlashComponent, TransformComponent> _query;

        public void Update(EcsWorld world, float deltaTime)
        {
            _query ??= world.CreateQuery<MuzzleFlashComponent, TransformComponent>();

            _query.ForEach((EntityId id,
                ref MuzzleFlashComponent flash,
                ref TransformComponent _) =>
            {
                flash.TimeInCurrentFrame += deltaTime;

                if (flash.TimeInCurrentFrame < flash.FrameDuration) return;

                flash.TimeInCurrentFrame -= flash.FrameDuration;
                flash.CurrentFrame++;

                if (flash.CurrentFrame >= flash.Frames.Length)
                {
                    world.DestroyEntity(id);
                    return;
                }

                if (flash.Renderer != null)
                    flash.Renderer.sprite = flash.Frames[flash.CurrentFrame];
            });
        }
    }
}
