using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using UnityEngine;

namespace Chronocaust.Ecs.Systems
{
    /// <summary>
    /// Drop active weapon and switch between the two inventory slots.
    /// </summary>
    public sealed class WeaponInventorySystem : IEcsUpdateSystem
    {
        private EcsQuery<PlayerTagComponent, TransformComponent, AimComponent, InputStateComponent,
            WeaponLoadoutComponent, EquippedWeaponComponent, WeaponCooldownComponent> _query;

        public void Update(EcsWorld world, float deltaTime)
        {
            _query ??= world.CreateQuery<PlayerTagComponent, TransformComponent, AimComponent,
                InputStateComponent, WeaponLoadoutComponent, EquippedWeaponComponent, WeaponCooldownComponent>();

            _query.ForEach((EntityId id,
                ref PlayerTagComponent _,
                ref TransformComponent transform,
                ref AimComponent aim,
                ref InputStateComponent input,
                ref WeaponLoadoutComponent loadout,
                ref EquippedWeaponComponent _,
                ref WeaponCooldownComponent _2) =>
            {
                if (transform.Transform == null)
                {
                    return;
                }

                int targetSlot = -1;
                if (input.SelectSlot0Pressed) targetSlot = 0;
                else if (input.SelectSlot1Pressed) targetSlot = 1;

                if (targetSlot >= 0 && targetSlot != loadout.ActiveIndex)
                {
                    WeaponInventoryUtility.SaveActiveSlotFromEntity(world, id, ref loadout);
                    WeaponInventoryUtility.ApplySlotToEntity(world, id, ref loadout, targetSlot);
                }

                if (input.DropPressed)
                {
                    WeaponInventoryUtility.SaveActiveSlotFromEntity(world, id, ref loadout);

                    ref WeaponSlotEntry active = ref WeaponInventoryUtility.GetSlot(ref loadout, loadout.ActiveIndex);
                    if (!active.HasWeapon)
                    {
                        return;
                    }

                    Vector2 dropPos = WeaponInventoryUtility.ComputeDropPosition(in transform, in aim);
                    world.CommandBuffer.EnqueueSpawnGroundWeapon(new GroundWeaponSpawnPayload
                    {
                        Position = dropPos,
                        Entry = active
                    });

                    active = WeaponSlotEntry.Empty;
                    WeaponInventoryUtility.ApplySlotToEntity(world, id, ref loadout, loadout.ActiveIndex);
                }
            });
        }
    }
}
