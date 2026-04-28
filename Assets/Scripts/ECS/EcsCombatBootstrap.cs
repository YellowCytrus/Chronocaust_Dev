using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using Chronocaust.Ecs.Systems;
using UnityEngine;

namespace Chronocaust.Ecs
{
    /// <summary>
    /// MonoBehaviour entry point. Builds the EcsWorld, registers systems, and creates the player entity.
    /// All entity creation uses ComponentSignature so entities land in the correct archetype from birth.
    /// </summary>
    public sealed class EcsCombatBootstrap : MonoBehaviour
    {
        [Header("Entity References")]
        [SerializeField] private Transform playerTransform;

        [Header("Starting Weapon")]
        [Tooltip("Leave empty if the player starts unarmed.")]
        [SerializeField] private WeaponDefinition startingWeapon;

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
            BindDestroyCallback(_world);
            RegisterSystems(_world);
            CreatePlayerEntity(_world);
            CreateGroundWeapons(_world);
            DisableLegacyMovementController();
        }

        private void Update() => _world?.Update(Time.deltaTime);
        private void FixedUpdate() => _world?.FixedUpdate(Time.fixedDeltaTime);

        // -----------------------------------------------------------------------
        // Destroy callback — keeps EcsWorld Core free of Unity types
        // -----------------------------------------------------------------------

        private static void BindDestroyCallback(EcsWorld world)
        {
            world.OnEntityDestroyed += id =>
            {
                if (world.IsAlive(id) == false) return;
                // At the moment of the callback the entity is still alive (data not yet removed).
                // We read TransformComponent and destroy the linked GameObject.
                ref TransformComponent tc = ref world.GetComponent<TransformComponent>(id);
                if (tc.Transform != null)
                {
                    Object.Destroy(tc.Transform.gameObject);
                }
            };
        }

        // -----------------------------------------------------------------------
        // Systems — declared in execution order
        // -----------------------------------------------------------------------

        private static void RegisterSystems(EcsWorld world)
        {
            // Simulation — Update
            world.AddSystem(new PlayerInputSystem());
            world.AddSystem(new PlayerAimSystem());
            world.AddSystem(new WeaponPickupSystem());
            world.AddSystem(new WeaponShootSystem());
            world.AddSystem(new ProjectileLifetimeSystem());

            // Rendering — Update
            world.AddSystem(new WeaponViewSystem());
            world.AddSystem(new CharacterAnimationSystem());

            // Simulation — FixedUpdate
            world.AddSystem(new PlayerMovementSystem());
            world.AddSystem(new ProjectileMovementSystem());
        }

        // -----------------------------------------------------------------------
        // Player entity creation
        // -----------------------------------------------------------------------

        private void CreatePlayerEntity(EcsWorld world)
        {
            // Build the exact archetype signature upfront — entity is born in the right archetype.
            ComponentSignature sig = ComponentSignature.Empty
                .With<PlayerTagComponent>()
                .With<TransformComponent>()
                .With<InputStateComponent>()
                .With<AimComponent>()
                .With<MovementComponent>()
                .With<EquippedWeaponComponent>()
                .With<WeaponCooldownComponent>()
                .With<WeaponViewComponent>();

            Rigidbody2D rb = playerTransform.GetComponent<Rigidbody2D>();
            if (rb != null) sig = sig.With<RigidbodyComponent>();
            else Debug.LogError("EcsCombatBootstrap: Rigidbody2D required on playerTransform.");

            IsometricCharacterRenderer isoRenderer =
                playerTransform.GetComponentInChildren<IsometricCharacterRenderer>();
            if (isoRenderer != null) sig = sig.With<CharacterRenderComponent>();

            EntityId player = world.CreateEntity(sig);

            // Fill component data by ref — direct chunk access, no boxing in the hot path.
            world.GetComponent<TransformComponent>(player).Transform = playerTransform;
            world.GetComponent<AimComponent>(player).Direction = Vector2.right;

            ref MovementComponent movement = ref world.GetComponent<MovementComponent>(player);
            PlayerAuthoring authoring = playerTransform.GetComponent<PlayerAuthoring>();
            if (authoring != null)
            {
                movement.BaseSpeed = authoring.BaseMovementSpeed;
                movement.UseIsometricAxes = authoring.UseIsometricAxes;
                movement.IsometricRightAxis = authoring.IsometricRightAxis;
                movement.IsometricUpAxis = authoring.IsometricUpAxis;
            }
            else
            {
                Debug.LogWarning("EcsCombatBootstrap: PlayerAuthoring not found on playerTransform. Using defaults.");
                movement.BaseSpeed = 5f;
                movement.UseIsometricAxes = true;
                movement.IsometricRightAxis = new Vector2(1f, 0.5f);
                movement.IsometricUpAxis = new Vector2(-1f, 0.5f);
            }

            if (startingWeapon != null)
            {
                ref EquippedWeaponComponent equipped = ref world.GetComponent<EquippedWeaponComponent>(player);
                equipped.WeaponSprite = startingWeapon.WeaponSprite;
                equipped.ProjectileSprite = startingWeapon.ProjectileSprite;
                equipped.WeaponSpriteScale = startingWeapon.WeaponSpriteScale;
                equipped.ProjectileSpriteScale = startingWeapon.ProjectileSpriteScale;
                equipped.WeaponSortingOrder = startingWeapon.WeaponSortingOrder;
                equipped.ProjectileSortingOrder = startingWeapon.ProjectileSortingOrder;
                equipped.FireRate = startingWeapon.FireRate;
                equipped.ProjectileSpeed = startingWeapon.ProjectileSpeed;
                equipped.ProjectileLifetime = startingWeapon.ProjectileLifetime;
                equipped.MuzzleOffset = startingWeapon.MuzzleOffset;
                equipped.MovementSpeedMultiplier = startingWeapon.MovementSpeedMultiplier;
            }

            // Initialise WeaponView GameObject here (not lazily inside a system).
            WeaponViewComponent view = CreateWeaponViewObject(playerTransform);
            ref WeaponViewComponent weaponView = ref world.GetComponent<WeaponViewComponent>(player);
            weaponView = view;

            if (rb != null)
                world.GetComponent<RigidbodyComponent>(player).Rigidbody = rb;

            if (isoRenderer != null)
                world.GetComponent<CharacterRenderComponent>(player).Renderer = isoRenderer;
        }

        private static WeaponViewComponent CreateWeaponViewObject(Transform parent)
        {
            GameObject go = new GameObject("WeaponView");
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.enabled = false;
            sr.sortingOrder = 100;

            return new WeaponViewComponent { Transform = go.transform, Renderer = sr };
        }

        private void CreateGroundWeapons(EcsWorld world)
        {
            GroundWeaponAuthoring[] authorings =
                FindObjectsByType<GroundWeaponAuthoring>(FindObjectsSortMode.None);
            if (authorings == null || authorings.Length == 0)
            {
                return;
            }

            ComponentSignature signature = ComponentSignature.Empty
                .With<GroundWeaponTagComponent>()
                .With<TransformComponent>()
                .With<WeaponComponent>();

            int count = authorings.Length;
            for (int i = 0; i < count; i++)
            {
                GroundWeaponAuthoring authoring = authorings[i];
                if (authoring == null || authoring.Definition == null)
                {
                    Debug.LogWarning("EcsCombatBootstrap: skipped GroundWeaponAuthoring with missing WeaponDefinition.");
                    continue;
                }

                EntityId id = world.CreateEntity(signature);
                world.GetComponent<TransformComponent>(id).Transform = authoring.transform;

                ref WeaponComponent weapon = ref world.GetComponent<WeaponComponent>(id);
                weapon.WeaponSprite = authoring.Definition.WeaponSprite;
                weapon.ProjectileSprite = authoring.Definition.ProjectileSprite;
                weapon.WeaponSpriteScale = authoring.Definition.WeaponSpriteScale;
                weapon.ProjectileSpriteScale = authoring.Definition.ProjectileSpriteScale;
                weapon.WeaponSortingOrder = authoring.Definition.WeaponSortingOrder;
                weapon.ProjectileSortingOrder = authoring.Definition.ProjectileSortingOrder;
                weapon.FireRate = authoring.Definition.FireRate;
                weapon.ProjectileSpeed = authoring.Definition.ProjectileSpeed;
                weapon.ProjectileLifetime = authoring.Definition.ProjectileLifetime;
                weapon.MuzzleOffset = authoring.Definition.MuzzleOffset;
                weapon.MovementSpeedMultiplier = authoring.Definition.MovementSpeedMultiplier;
            }
        }

        private void DisableLegacyMovementController()
        {
            // Legacy MonoBehaviour kept for reference; ECS owns movement when bootstrap is active.
            IsometricPlayerMovementController legacy =
                playerTransform.GetComponent<IsometricPlayerMovementController>();
            if (legacy != null) legacy.enabled = false;
        }
    }
}
