using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using UnityEngine;

namespace Chronocaust.Ecs.Systems
{
    public sealed class PlayerMovementSystem : IEcsFixedUpdateSystem
    {
        public void FixedUpdate(EcsWorld world, float deltaTime)
        {
            foreach (EcsEntity entity in world.Entities)
            {
                if (!entity.Has<PlayerTagComponent>() ||
                    !entity.TryGet(out InputStateComponent inputState) ||
                    !entity.TryGet(out MovementComponent movementComponent) ||
                    !entity.TryGet(out RigidbodyComponent rigidbodyComponent) ||
                    rigidbodyComponent.Rigidbody == null)
                {
                    continue;
                }

                Vector2 movementInput = inputState.MoveInput;
                Vector2 movementDirection = movementComponent.UseIsometricAxes
                    ? ToIsometricDirection(
                        movementInput,
                        movementComponent.IsometricRightAxis,
                        movementComponent.IsometricUpAxis
                    )
                    : movementInput;
                Vector2 movement = movementDirection * movementComponent.Speed;
                Vector2 currentPosition = rigidbodyComponent.Rigidbody.position;
                Vector2 newPosition = currentPosition + movement * deltaTime;
                rigidbodyComponent.Rigidbody.MovePosition(newPosition);

                if (entity.TryGet(out CharacterRenderComponent renderComponent) &&
                    renderComponent.Renderer != null)
                {
                    renderComponent.Renderer.SetDirection(movementDirection);
                }
            }
        }

        private static Vector2 ToIsometricDirection(Vector2 gridInput, Vector2 rightAxis, Vector2 upAxis)
        {
            Vector2 right = rightAxis.sqrMagnitude > 0.0001f ? rightAxis.normalized : Vector2.right;
            Vector2 up = upAxis.sqrMagnitude > 0.0001f ? upAxis.normalized : Vector2.up;
            Vector2 iso = right * gridInput.x + up * gridInput.y;

            if (iso.sqrMagnitude > 1f)
            {
                iso.Normalize();
            }

            return iso;
        }
    }
}
