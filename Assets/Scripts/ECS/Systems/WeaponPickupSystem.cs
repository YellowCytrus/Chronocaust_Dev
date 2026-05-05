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

        private struct PendingPickup
        {
            public EntityId        PlayerId;
            public ComponentSignature OldSig;   // snapshot of the player's archetype at pickup time
            public WeaponComponent NewWeapon;
        }

        private readonly List<PendingPickup> _pendingPickups = new List<PendingPickup>(4);

        public void Update(EcsWorld world, float deltaTime)
        {
            _groundQuery ??= world.CreateQuery<GroundWeaponTagComponent, TransformComponent, WeaponComponent>();
            _playerQuery ??= world.CreateQuery<PlayerTagComponent, TransformComponent, InputStateComponent, EquippedWeaponComponent>();

            RebuildGroundWeaponGrid();

            if (_groundWeapons.Count == 0)
            {
                return;
            }

            _pendingPickups.Clear();

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
                _pendingPickups.Add(new PendingPickup
                {
                    PlayerId  = playerId,
                    OldSig    = world.GetSignature(playerId),
                    NewWeapon = picked.Weapon
                });
                equipped = EquippedWeaponComponent.From(picked.Weapon);
                world.CommandBuffer.DestroyEntity(picked.Id);
            });

            // Structural changes (add/remove tag components) must happen outside ForEach.
            int count = _pendingPickups.Count;
            for (int i = 0; i < count; i++)
            {
                PendingPickup p = _pendingPickups[i];
                RemoveWeaponTypeTags(world, p.PlayerId, p.OldSig);
                AddWeaponTypeTags(world, p.PlayerId, in p.NewWeapon);
            }
        }

        /// <summary>
        /// Removes whichever weapon-type tag+data components the entity currently carries.
        /// Uses the archived signature snapshot so we don't branch on a data field.
        /// </summary>
        private static void RemoveWeaponTypeTags(EcsWorld world, EntityId id, ComponentSignature sig)
        {
            if (sig.Has<ShotgunTagComponent>())
            {
                world.CommandBuffer.RemoveComponent<ShotgunTagComponent>(id);
                world.CommandBuffer.RemoveComponent<ShotgunDataComponent>(id);
            }
            else if (sig.Has<LaserTagComponent>())
            {
                world.CommandBuffer.RemoveComponent<LaserTagComponent>(id);
                world.CommandBuffer.RemoveComponent<LaserDataComponent>(id);
            }
            else if (sig.Has<MeleeTagComponent>())
            {
                world.CommandBuffer.RemoveComponent<MeleeTagComponent>(id);
                world.CommandBuffer.RemoveComponent<MeleeDataComponent>(id);
            }
        }

        private static void AddWeaponTypeTags(EcsWorld world, EntityId id, in WeaponComponent w)
        {
            switch (w.Kind)
            {
                case Ecs.WeaponKind.Shotgun:
                    world.CommandBuffer.AddComponent(id, new ShotgunTagComponent());
                    world.CommandBuffer.AddComponent(id, new ShotgunDataComponent
                    {
                        PelletCount = w.PelletCount > 0 ? w.PelletCount : 8,
                        SpreadAngle = w.SpreadAngle
                    });
                    break;
                case Ecs.WeaponKind.Laser:
                    world.CommandBuffer.AddComponent(id, new LaserTagComponent());
                    world.CommandBuffer.AddComponent(id, new LaserDataComponent
                    {
                        BeamDuration = w.BeamDuration > 0f ? w.BeamDuration : 0.3f,
                        BeamWidth    = w.BeamWidth > 0f ? w.BeamWidth : 0.1f,
                        BeamColor    = w.BeamColor
                    });
                    break;
                case Ecs.WeaponKind.Melee:
                    world.CommandBuffer.AddComponent(id, new MeleeTagComponent());
                    world.CommandBuffer.AddComponent(id, new MeleeDataComponent
                    {
                        Range    = w.MeleeRange > 0f ? w.MeleeRange : 1.5f,
                        ArcAngle = w.MeleeArcAngle > 0f ? w.MeleeArcAngle : 90f
                    });
                    break;
                // WeaponKind.Default: no tag, WeaponShootSystem handles via .Excluding<>
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
