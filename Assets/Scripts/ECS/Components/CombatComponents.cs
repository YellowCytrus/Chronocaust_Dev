using Chronocaust.Ecs.Core;
using UnityEngine;

namespace Chronocaust.Ecs.Components
{
    public sealed class PlayerTagComponent : IEcsComponent
    {
    }

    public sealed class TransformComponent : IEcsComponent
    {
        public Transform Transform;
    }

    public sealed class InputStateComponent : IEcsComponent
    {
        public bool FirePressed;
        public Vector3 MouseWorldPosition;
        public Vector2 MoveInput;
    }

    public sealed class AimComponent : IEcsComponent
    {
        public Vector2 Direction = Vector2.right;
    }

    public sealed class WeaponComponent : IEcsComponent
    {
        public Sprite WeaponSprite;
        public Sprite ProjectileSprite;
        public float FireRate = 6f;
        public float ProjectileSpeed = 10f;
        public float ProjectileLifetime = 2f;
        public Vector2 MuzzleOffset = new Vector2(0.45f, 0f);
    }

    public sealed class WeaponCooldownComponent : IEcsComponent
    {
        public float NextShotTime;
    }

    public sealed class WeaponViewComponent : IEcsComponent
    {
        public Transform Transform;
        public SpriteRenderer Renderer;
    }

    public sealed class ProjectileComponent : IEcsComponent
    {
        public Vector2 Direction;
        public float Speed;
        public float TimeLeft;
    }

    public sealed class RigidbodyComponent : IEcsComponent
    {
        public Rigidbody2D Rigidbody;
    }

    public sealed class MovementComponent : IEcsComponent
    {
        public float Speed = 1f;
        public bool UseIsometricAxes = true;
        public Vector2 IsometricRightAxis = new Vector2(1f, 0.5f);
        public Vector2 IsometricUpAxis = new Vector2(-1f, 0.5f);
    }

    public sealed class CharacterRenderComponent : IEcsComponent
    {
        public IsometricCharacterRenderer Renderer;
    }
}
