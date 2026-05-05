using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;

namespace Chronocaust.Ecs.Systems
{
    /// <summary>
    /// Consumes CommandBuffer.ShotEvents and applies a recoil impulse to each shooter's
    /// RecoilComponent. Completely decoupled from weapon type: weapon systems only call
    /// CommandBuffer.RecordShotEvent — they know nothing about recoil.
    /// Any future per-shot system (camera shake, audio, etc.) reads the same ShotEvents.
    /// </summary>
    public sealed class RecoilApplySystem : IEcsUpdateSystem
    {
        public void Update(EcsWorld world, float deltaTime)
        {
            var events = world.CommandBuffer.ShotEvents;
            int count = events.Count;
            if (count == 0) return;

            for (int i = 0; i < count; i++)
            {
                CommandBuffer.ShotEvent ev = events[i];
                if (!world.IsAlive(ev.Shooter)) continue;

                ref EquippedWeaponComponent equipped = ref world.GetComponent<EquippedWeaponComponent>(ev.Shooter);
                if (equipped.RecoilStrength <= 0f) continue;

                ref RecoilComponent recoil = ref world.GetComponent<RecoilComponent>(ev.Shooter);
                recoil.CurrentVelocity += -ev.Direction * equipped.RecoilStrength;
            }
        }
    }
}
