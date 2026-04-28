using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using UnityEngine;

namespace Chronocaust.Ecs.Systems
{
    /// <summary>
    /// Rendering layer: updates weapon sprite position, rotation, and flip.
    /// WeaponViewComponent.Transform/Renderer must be initialised in EcsCombatBootstrap before first frame.
    /// </summary>
    public sealed class WeaponViewSystem : IEcsUpdateSystem
    {
        private EcsQuery<AimComponent, EquippedWeaponComponent, WeaponViewComponent> _query;

        public void Update(EcsWorld world, float deltaTime)
        {
            _query ??= world.CreateQuery<AimComponent, EquippedWeaponComponent, WeaponViewComponent>();

            _query.ForEach((EntityId id,
                ref AimComponent aim,
                ref EquippedWeaponComponent equipped,
                ref WeaponViewComponent view) =>
            {
                if (view.Transform == null || view.Renderer == null) return;

                if (!equipped.HasWeapon)
                {
                    view.Renderer.enabled = false;
                    return;
                }

                view.Renderer.enabled = true;
                view.Renderer.sprite = equipped.WeaponSprite;
                view.Renderer.sortingOrder = equipped.WeaponSortingOrder;
                float scale = equipped.WeaponSpriteScale > 0f ? equipped.WeaponSpriteScale : 1f;
                view.Transform.localScale = new Vector3(scale, scale, 1f);
                Vector2 dir = aim.Direction.sqrMagnitude > 0.0001f ? aim.Direction : Vector2.right;
                view.Transform.localPosition = new Vector3(equipped.MuzzleOffset.x, equipped.MuzzleOffset.y, 0f);
                view.Transform.rotation = Quaternion.Euler(0f, 0f,
                    Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                view.Renderer.flipY = dir.x < 0f;
            });
        }
    }
}
