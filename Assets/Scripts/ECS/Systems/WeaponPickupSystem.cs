using System.Collections.Generic;
using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using UnityEngine;

namespace Chronocaust.Ecs.Systems
{
    public sealed class WeaponPickupSystem : IEcsUpdateSystem
    {
        private const float CellSize = 2f;

        private struct GroundWeaponSnapshot
        {
            public EntityId Id;
            public Vector2 Position;
            public WeaponComponent Weapon;
        }

        private EcsQuery<GroundWeaponTagComponent, TransformComponent, WeaponComponent> _groundQuery;
        private EcsQuery<PlayerTagComponent, TransformComponent, AimComponent, InputStateComponent,
            WeaponLoadoutComponent, EquippedWeaponComponent> _playerQuery;

        private readonly List<GroundWeaponSnapshot> _groundWeapons = new List<GroundWeaponSnapshot>(64);
        private readonly Dictionary<int, List<int>> _cellToWeaponIndices = new Dictionary<int, List<int>>(64);

        private struct PendingPickup
        {
            public EntityId PlayerId;
            public int TargetSlot;
            public WeaponComponent NewWeapon;
            public WeaponSlotEntry ReplacedEntry;
            public bool HadReplacement;
            public Vector2 DropPosition;
        }

        private readonly List<PendingPickup> _pendingPickups = new List<PendingPickup>(4);

        public void Update(EcsWorld world, float deltaTime)
        {
            _groundQuery ??= world.CreateQuery<GroundWeaponTagComponent, TransformComponent, WeaponComponent>();
            _playerQuery ??= world.CreateQuery<PlayerTagComponent, TransformComponent, AimComponent,
                InputStateComponent, WeaponLoadoutComponent, EquippedWeaponComponent>();

            RebuildGroundWeaponGrid();

            if (_groundWeapons.Count == 0)
            {
                return;
            }

            _pendingPickups.Clear();

            float pickupRadiusSq = WeaponInventoryUtility.PickupRadius * WeaponInventoryUtility.PickupRadius;
            _playerQuery.ForEach((EntityId playerId,
                ref PlayerTagComponent _,
                ref TransformComponent playerTransform,
                ref AimComponent aim,
                ref InputStateComponent input,
                ref WeaponLoadoutComponent loadout,
                ref EquippedWeaponComponent _) =>
            {
                if (!input.InteractPressed || playerTransform.Transform == null)
                {
                    return;
                }

                Vector2 playerPos = playerTransform.Transform.position;
                int bestIndex = FindBestWeaponIndex(playerPos, pickupRadiusSq);
                if (bestIndex < 0)
                {
                    return;
                }

                GroundWeaponSnapshot picked = _groundWeapons[bestIndex];
                bool activeWasEmpty = !WeaponInventoryUtility.GetSlotCopy(in loadout, loadout.ActiveIndex).HasWeapon;

                int targetSlot = WeaponInventoryUtility.FindFirstEmptySlot(in loadout);
                if (targetSlot < 0)
                {
                    targetSlot = loadout.ActiveIndex;
                }

                ref WeaponSlotEntry target = ref WeaponInventoryUtility.GetSlot(ref loadout, targetSlot);
                bool hadReplacement = target.HasWeapon;
                WeaponSlotEntry replaced = target;

                target = WeaponInventoryUtility.FromWeaponComponent(in picked.Weapon);

                _pendingPickups.Add(new PendingPickup
                {
                    PlayerId = playerId,
                    TargetSlot = targetSlot,
                    NewWeapon = picked.Weapon,
                    ReplacedEntry = replaced,
                    HadReplacement = hadReplacement,
                    DropPosition = WeaponInventoryUtility.ComputeDropPosition(in playerTransform, in aim)
                });

                world.CommandBuffer.DestroyEntity(picked.Id);

                if (targetSlot == loadout.ActiveIndex || activeWasEmpty)
                {
                    if (activeWasEmpty && targetSlot != loadout.ActiveIndex)
                    {
                        loadout.ActiveIndex = targetSlot;
                    }

                    WeaponInventoryUtility.ApplySlotToEntity(world, playerId, ref loadout, loadout.ActiveIndex);
                }
            });

            int count = _pendingPickups.Count;
            for (int i = 0; i < count; i++)
            {
                PendingPickup p = _pendingPickups[i];
                if (p.HadReplacement)
                {
                    world.CommandBuffer.EnqueueSpawnGroundWeapon(new GroundWeaponSpawnPayload
                    {
                        Position = p.DropPosition,
                        Entry = p.ReplacedEntry
                    });
                }
            }
        }

        private void RebuildGroundWeaponGrid()
        {
            _groundWeapons.Clear();
            _cellToWeaponIndices.Clear();

            _groundQuery.ForEach((EntityId id,
                ref GroundWeaponTagComponent _,
                ref TransformComponent transform,
                ref WeaponComponent weapon) =>
            {
                if (transform.Transform == null)
                {
                    return;
                }

                GroundWeaponSnapshot snapshot = new GroundWeaponSnapshot
                {
                    Id = id,
                    Position = transform.Transform.position,
                    Weapon = weapon
                };

                int index = _groundWeapons.Count;
                _groundWeapons.Add(snapshot);

                Vector2Int cell = ToCell(snapshot.Position);
                int key = CellKey(cell.x, cell.y);
                if (!_cellToWeaponIndices.TryGetValue(key, out List<int> list))
                {
                    list = new List<int>(4);
                    _cellToWeaponIndices[key] = list;
                }

                list.Add(index);
            });
        }

        private int FindBestWeaponIndex(Vector2 playerPos, float pickupRadiusSq)
        {
            Vector2Int center = ToCell(playerPos);
            int bestIndex = -1;
            float bestSq = pickupRadiusSq;

            for (int y = center.y - 1; y <= center.y + 1; y++)
            {
                for (int x = center.x - 1; x <= center.x + 1; x++)
                {
                    int key = CellKey(x, y);
                    if (!_cellToWeaponIndices.TryGetValue(key, out List<int> indices))
                    {
                        continue;
                    }

                    int count = indices.Count;
                    for (int i = 0; i < count; i++)
                    {
                        int weaponIndex = indices[i];
                        Vector2 delta = _groundWeapons[weaponIndex].Position - playerPos;
                        float sq = delta.sqrMagnitude;
                        if (sq <= bestSq)
                        {
                            bestSq = sq;
                            bestIndex = weaponIndex;
                        }
                    }
                }
            }

            return bestIndex;
        }

        private static Vector2Int ToCell(Vector2 position) =>
            new Vector2Int(
                Mathf.FloorToInt(position.x / CellSize),
                Mathf.FloorToInt(position.y / CellSize));

        private static int CellKey(int x, int y)
        {
            unchecked
            {
                return (x * 73856093) ^ (y * 19349663);
            }
        }
    }
}
