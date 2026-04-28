using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using Chronocaust.Ecs.Systems;
using UnityEngine;

namespace Chronocaust.Ecs
{
    public sealed class EcsCombatBootstrap : MonoBehaviour
    {
        [Header("Entity References")]
        [SerializeField] private Transform playerTransform;
        [SerializeField, Min(0.1f)] private float movementSpeed = 1f;
        [SerializeField] private bool useIsometricAxes = true;
        [SerializeField] private Vector2 isometricRightAxis = new Vector2(1f, 0.5f);
        [SerializeField] private Vector2 isometricUpAxis = new Vector2(-1f, 0.5f);

        [Header("Weapon Visuals")]
        [SerializeField] private Sprite weaponSprite;
        [SerializeField] private Sprite projectileSprite;

        [Header("Weapon Stats")]
        [SerializeField, Min(0.1f)] private float fireRate = 6f;
        [SerializeField, Min(0.1f)] private float projectileSpeed = 10f;
        [SerializeField, Min(0.1f)] private float projectileLifetime = 2f;
        [SerializeField] private Vector2 muzzleOffset = new Vector2(0.45f, 0f);

        private EcsWorld _world;

        private void Awake()
        {
            if (playerTransform == null)
            {
                Debug.LogError("EcsCombatBootstrap: playerTransform is not assigned.");
                enabled = false;
                return;
            }

            _world = new EcsWorld();
            RegisterSystems(_world);
            CreatePlayerEntity(_world);
            DisableLegacyMovementController();
        }

        private void Update()
        {
            _world?.Update(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            _world?.FixedUpdate(Time.fixedDeltaTime);
        }

        private void RegisterSystems(EcsWorld world)
        {
            world.AddSystem(new PlayerInputSystem());
            world.AddSystem(new PlayerAimSystem());
            world.AddSystem(new WeaponViewSystem());
            world.AddSystem(new WeaponShootSystem());
            world.AddSystem(new ProjectileLifetimeSystem());
            world.AddSystem(new PlayerMovementSystem());
            world.AddSystem(new ProjectileMovementSystem());
        }

        private void CreatePlayerEntity(EcsWorld world)
        {
            EcsEntity player = world.CreateEntity();
            player.Add(new PlayerTagComponent());
            player.Add(new TransformComponent
            {
                Transform = playerTransform
            });
            player.Add(new InputStateComponent());
            player.Add(new MovementComponent
            {
                Speed = movementSpeed,
                UseIsometricAxes = useIsometricAxes,
                IsometricRightAxis = isometricRightAxis,
                IsometricUpAxis = isometricUpAxis
            });
            player.Add(new AimComponent());
            player.Add(new WeaponComponent
            {
                WeaponSprite = weaponSprite,
                ProjectileSprite = projectileSprite,
                FireRate = fireRate,
                ProjectileSpeed = projectileSpeed,
                ProjectileLifetime = projectileLifetime,
                MuzzleOffset = muzzleOffset
            });
            player.Add(new WeaponCooldownComponent());
            player.Add(new WeaponViewComponent());

            Rigidbody2D playerRigidbody = playerTransform.GetComponent<Rigidbody2D>();
            if (playerRigidbody != null)
            {
                player.Add(new RigidbodyComponent
                {
                    Rigidbody = playerRigidbody
                });
            }
            else
            {
                Debug.LogError("EcsCombatBootstrap: playerTransform must have Rigidbody2D for ECS movement.");
            }

            IsometricCharacterRenderer isoRenderer = playerTransform.GetComponentInChildren<IsometricCharacterRenderer>();
            if (isoRenderer != null)
            {
                player.Add(new CharacterRenderComponent
                {
                    Renderer = isoRenderer
                });
            }
        }

        private void DisableLegacyMovementController()
        {
            IsometricPlayerMovementController legacyController = playerTransform.GetComponent<IsometricPlayerMovementController>();
            if (legacyController != null)
            {
                legacyController.enabled = false;
            }
        }
    }
}
