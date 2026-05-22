using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using UnityEngine;
using WeaponKind = Chronocaust.Ecs.WeaponKind;

namespace Chronocaust.Ecs.Systems
{
    /// <summary>
    /// Shows floating world-space prompts above ground weapons when the player is nearby.
    /// </summary>
    public sealed class GroundWeaponHintSystem : IEcsUpdateSystem
    {
        private const float HintRadius = WeaponInventoryUtility.PickupRadius;

        private EcsQuery<GroundWeaponTagComponent, TransformComponent, WeaponComponent,
            GroundWeaponHintViewComponent> _groundQuery;
        private EcsQuery<PlayerTagComponent, TransformComponent> _playerQuery;

        private Vector2 _playerPos;
        private bool _hasPlayer;

        public void Update(EcsWorld world, float deltaTime)
        {
            _groundQuery ??= world.CreateQuery<GroundWeaponTagComponent, TransformComponent, WeaponComponent,
                GroundWeaponHintViewComponent>();
            _playerQuery ??= world.CreateQuery<PlayerTagComponent, TransformComponent>();

            _hasPlayer = false;
            _playerQuery.ForEach((EntityId _,
                ref PlayerTagComponent __,
                ref TransformComponent transform) =>
            {
                if (transform.Transform != null)
                {
                    _playerPos = transform.Transform.position;
                    _hasPlayer = true;
                }
            });

            if (!_hasPlayer)
            {
                return;
            }

            float radiusSq = HintRadius * HintRadius;
            Camera cam = Camera.main;

            _groundQuery.ForEach((EntityId _,
                ref GroundWeaponTagComponent __,
                ref TransformComponent transform,
                ref WeaponComponent weapon,
                ref GroundWeaponHintViewComponent hint) =>
            {
                if (hint.Root == null || hint.Label == null || transform.Transform == null)
                {
                    return;
                }

                Vector2 pos = transform.Transform.position;
                bool inRange = (pos - _playerPos).sqrMagnitude <= radiusSq;
                hint.Root.gameObject.SetActive(inRange);

                if (!inRange)
                {
                    return;
                }

                string name = ResolveWeaponLabel(in weapon);
                hint.Label.text = $"[E]  {name}";

                if (cam != null)
                {
                    hint.Root.rotation = Quaternion.LookRotation(
                        hint.Root.position - cam.transform.position,
                        Vector3.up);
                }
            });
        }

        private static string ResolveWeaponLabel(in WeaponComponent weapon)
        {
            if (!string.IsNullOrWhiteSpace(weapon.DisplayName))
            {
                return weapon.DisplayName;
            }

            switch (weapon.Kind)
            {
                case WeaponKind.Shotgun: return "Shotgun";
                case WeaponKind.Laser: return "Laser";
                case WeaponKind.Melee: return "Melee";
                default: return "Weapon";
            }
        }
    }
}
