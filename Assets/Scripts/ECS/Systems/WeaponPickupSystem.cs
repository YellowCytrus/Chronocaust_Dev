using System.Collections.Generic;
using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using UnityEngine;

namespace Chronocaust.Ecs.Systems
{
    public sealed class WeaponPickupSystem : IEcsUpdateSystem
    {
        private const float PickupRadius = 1.5f;
        private const float CellSize = 2f;

        private struct GroundWeaponSnapshot
        {
            public EntityId Id;
            public Vector2 Position;
            public WeaponComponent Weapon;
        }

        private EcsQuery<GroundWeaponTagComponent, TransformComponent, WeaponComponent> _groundQuery;
        private EcsQuery<PlayerTagComponent, TransformComponent, InputStateComponent, EquippedWeaponComponent> _playerQuery;

        private readonly List<GroundWeaponSnapshot> _groundWeapons = new List<GroundWeaponSnapshot>(64);
        private readonly Dictionary<int, List<int>> _cellToWeaponIndices = new Dictionary<int, List<int>>(64);

        public void Update(EcsWorld world, float deltaTime)
        {
            _groundQuery ??= world.CreateQuery<GroundWeaponTagComponent, TransformComponent, WeaponComponent>();
            _playerQuery ??= world.CreateQuery<PlayerTagComponent, TransformComponent, InputStateComponent, EquippedWeaponComponent>();

            RebuildGroundWeaponGrid();

            if (_groundWeapons.Count == 0)
            {
                return;
            }

            float pickupRadiusSq = PickupRadius * PickupRadius;
            _playerQuery.ForEach((EntityId playerId,
                ref PlayerTagComponent _,
                ref TransformComponent playerTransform,
                ref InputStateComponent input,
                ref EquippedWeaponComponent equipped) =>
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
                equipped = EquippedWeaponComponent.From(picked.Weapon);
                world.CommandBuffer.DestroyEntity(picked.Id);
            });
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

        private static Vector2Int ToCell(Vector2 position)
        {
            return new Vector2Int(
                Mathf.FloorToInt(position.x / CellSize),
                Mathf.FloorToInt(position.y / CellSize));
        }

        private static int CellKey(int x, int y)
        {
            unchecked
            {
                return (x * 73856093) ^ (y * 19349663);
            }
        }
    }
}
