using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using UnityEngine;

namespace Chronocaust.Ecs
{
    /// <summary>
    /// Domain utility for synchronous health mutation. Not an ECS system.
    /// Future: may publish DamageEvent instead of writing HP directly.
    /// </summary>
    public static class CombatDamage
    {
        public static bool Apply(EcsWorld world, EntityId instigator, EntityId target, float amount)
        {
            if (amount <= 0f || !world.IsAlive(target))
            {
                return false;
            }

            if (!CombatTargetRules.CanDamage(world, instigator, target))
            {
                return false;
            }

            ComponentSignature sig = world.GetSignature(target);
            if (!sig.Has<HealthComponent>())
            {
                return false;
            }

            ref HealthComponent health = ref world.GetComponent<HealthComponent>(target);
            health.Current = Mathf.Max(0f, health.Current - amount);
            return true;
        }
    }
}
