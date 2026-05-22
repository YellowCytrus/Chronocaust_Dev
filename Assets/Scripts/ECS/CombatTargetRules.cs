using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;

namespace Chronocaust.Ecs
{
    /// <summary>
    /// Faction and targeting rules — separate from damageability (HealthComponent).
    /// </summary>
    public static class CombatTargetRules
    {
        public static bool CanDamage(EcsWorld world, EntityId instigator, EntityId target)
        {
            if (!world.IsAlive(instigator) || !world.IsAlive(target))
            {
                return false;
            }

            if (instigator == target)
            {
                return false;
            }

            ComponentSignature instigatorSig = world.GetSignature(instigator);
            ComponentSignature targetSig = world.GetSignature(target);

            if (instigatorSig.Has<PlayerTagComponent>() && targetSig.Has<PlayerTagComponent>())
            {
                return false;
            }

            return true;
        }
    }
}
