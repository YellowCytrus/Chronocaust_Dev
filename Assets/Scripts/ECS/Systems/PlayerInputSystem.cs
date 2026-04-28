using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using UnityEngine;

namespace Chronocaust.Ecs.Systems
{
    public sealed class PlayerInputSystem : IEcsUpdateSystem
    {
        public void Update(EcsWorld world, float deltaTime)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            foreach (EcsEntity entity in world.Entities)
            {
                if (!entity.Has<PlayerTagComponent>() ||
                    !entity.TryGet(out InputStateComponent inputState) ||
                    !entity.TryGet(out TransformComponent transformComponent) ||
                    transformComponent.Transform == null)
                {
                    continue;
                }

                Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                mouseWorld.z = transformComponent.Transform.position.z;

                inputState.MouseWorldPosition = mouseWorld;
                inputState.FirePressed = Input.GetMouseButton(0);
                inputState.MoveInput = Vector2.ClampMagnitude(
                    new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")),
                    1f
                );
            }
        }
    }
}
