using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using UnityEngine;

namespace Chronocaust.Ecs.Systems
{
    /// <summary>
    /// Simulation layer: applies velocity to rigidbody, writes LastDirection for CharacterAnimationSystem.
    /// Does NOT call any rendering methods.
    /// </summary>
    public sealed class PlayerMovementSystem : IEcsFixedUpdateSystem
    {
        private EcsQuery<PlayerTagComponent, InputStateComponent, MovementComponent,
            EquippedWeaponComponent, RigidbodyComponent, RecoilComponent> _query;

        public void FixedUpdate(EcsWorld world, float deltaTime)
        {
            _query ??= world.CreateQuery<PlayerTagComponent, InputStateComponent, MovementComponent,
                EquippedWeaponComponent, RigidbodyComponent, RecoilComponent>();

            _query.ForEach((EntityId id,
                ref PlayerTagComponent _,
                ref InputStateComponent input,
                ref MovementComponent movement,
                ref EquippedWeaponComponent equipped,
                ref RigidbodyComponent rb,
                ref RecoilComponent recoil) =>
            {
                if (rb.Rigidbody == null) return;

                Vector2 dir = movement.UseIsometricAxes
                    ? ToIsometricDirection(input.MoveInput, movement.IsometricRightAxis, movement.IsometricUpAxis)
                    : input.MoveInput;

                float multiplier = equipped.HasWeapon ? equipped.MovementSpeedMultiplier : 1f;
                rb.Rigidbody.linearVelocity = dir * (movement.BaseSpeed * multiplier) + recoil.CurrentVelocity;
                movement.LastDirection = dir;
            });
        }

        private static Vector2 ToIsometricDirection(Vector2 input, Vector2 right, Vector2 up)
        {
            Vector2 r = right.sqrMagnitude > 0.0001f ? right.normalized : Vector2.right;
            Vector2 u = up.sqrMagnitude > 0.0001f ? up.normalized : Vector2.up;
            Vector2 iso = r * input.x + u * input.y;
            // Iso axes are non-orthogonal, so |r*x + u*y| depends on the angle between them
            // (drops below 1 on diagonals). Rescale to preserve input magnitude => uniform speed.
            float sqr = iso.sqrMagnitude;
            if (sqr > 0.0001f)
            {
                iso *= input.magnitude / Mathf.Sqrt(sqr);
            }
            return iso;
        }
    }
}
