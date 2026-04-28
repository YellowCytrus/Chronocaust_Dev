using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using UnityEngine;

namespace Chronocaust.Ecs.Systems
{
    public sealed class WeaponViewSystem : IEcsUpdateSystem
    {
        public void Update(EcsWorld world, float deltaTime)
        {
            foreach (EcsEntity entity in world.Entities)
            {
                if (!entity.TryGet(out TransformComponent ownerTransform) ||
                    !entity.TryGet(out AimComponent aimComponent) ||
                    !entity.TryGet(out WeaponComponent weaponComponent) ||
                    !entity.TryGet(out WeaponViewComponent weaponView) ||
                    ownerTransform.Transform == null)
                {
                    continue;
                }

                if (weaponView.Transform == null)
                {
                    GameObject weaponObject = new GameObject("WeaponView");
                    weaponObject.transform.SetParent(ownerTransform.Transform);
                    weaponView.Transform = weaponObject.transform;
                    weaponView.Renderer = weaponObject.AddComponent<SpriteRenderer>();
                    weaponView.Renderer.sortingOrder = 100;
                }

                weaponView.Renderer.sprite = weaponComponent.WeaponSprite;

                Vector2 offset = weaponComponent.MuzzleOffset;
                Vector2 direction = aimComponent.Direction.sqrMagnitude > 0.0001f ? aimComponent.Direction : Vector2.right;
                weaponView.Transform.localPosition = new Vector3(offset.x, offset.y, 0f);
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                weaponView.Transform.rotation = Quaternion.Euler(0f, 0f, angle);

                // Flip weapon vertically to avoid upside-down sprites when aiming left.
                weaponView.Renderer.flipY = direction.x < 0f;
            }
        }
    }
}
