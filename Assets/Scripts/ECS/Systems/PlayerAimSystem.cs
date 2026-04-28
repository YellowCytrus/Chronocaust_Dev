using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using UnityEngine;

namespace Chronocaust.Ecs.Systems
{
    public sealed class PlayerAimSystem : IEcsUpdateSystem
    {
        private EcsQuery<PlayerTagComponent, TransformComponent, InputStateComponent, AimComponent> _query;

        public void Update(EcsWorld world, float deltaTime)
        {
            _query ??= world.CreateQuery<PlayerTagComponent, TransformComponent, InputStateComponent, AimComponent>();

            _query.ForEach((EntityId id,
                ref PlayerTagComponent _,
                ref TransformComponent transform,
                ref InputStateComponent input,
                ref AimComponent aim) =>
            {
                if (transform.Transform == null) return;

                Vector2 dir = (Vector2)input.MouseWorldPosition - (Vector2)transform.Transform.position;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    aim.Direction = dir.normalized;
                }
            });
        }
    }
}
