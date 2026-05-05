using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using UnityEngine;

namespace Chronocaust.Ecs.Systems
{
    /// <summary>
    /// Decays RecoilComponent.CurrentVelocity each physics step using exponential decay.
    /// Must run before PlayerMovementSystem so the decayed value is applied this frame.
    /// </summary>
    public sealed class RecoilDecaySystem : IEcsFixedUpdateSystem
    {
        private const float DefaultDecayRate = 8f;

        private EcsQuery<RecoilComponent, EquippedWeaponComponent> _query;

        public void FixedUpdate(EcsWorld world, float deltaTime)
        {
            _query ??= world.CreateQuery<RecoilComponent, EquippedWeaponComponent>();

            _query.ForEach((EntityId _,
                ref RecoilComponent recoil,
                ref EquippedWeaponComponent equipped) =>
            {
                if (recoil.CurrentVelocity.sqrMagnitude < 0.0001f)
                {
                    recoil.CurrentVelocity = Vector2.zero;
                    return;
                }

                float rate = equipped.RecoilDecayRate > 0f ? equipped.RecoilDecayRate : DefaultDecayRate;
                recoil.CurrentVelocity *= Mathf.Exp(-rate * deltaTime);
            });
        }
    }
}
