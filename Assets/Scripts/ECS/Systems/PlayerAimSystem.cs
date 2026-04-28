using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using UnityEngine;

namespace Chronocaust.Ecs.Systems
{
    public sealed class PlayerAimSystem : IEcsUpdateSystem
    {
        public void Update(EcsWorld world, float deltaTime)
        {
            foreach (EcsEntity entity in world.Entities)
            {
                if (!entity.Has<PlayerTagComponent>() ||
                    !entity.TryGet(out TransformComponent transformComponent) ||
                    !entity.TryGet(out InputStateComponent inputState) ||
                    !entity.TryGet(out AimComponent aimComponent) ||
                    transformComponent.Transform == null)
                {
                    continue;
                }

                Vector2 origin = transformComponent.Transform.position;
                Vector2 target = inputState.MouseWorldPosition;
                Vector2 dir = (target - origin);

                if (dir.sqrMagnitude > 0.0001f)
                {
                    aimComponent.Direction = dir.normalized;
                }
            }
        }
    }
}
