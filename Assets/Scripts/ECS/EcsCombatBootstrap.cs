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

        [Header("HUD")]
        [SerializeField] private PlayerHudAuthoring hudAuthoring;

        private EcsWorld _world;

        private void Awake()
        {
            if (playerTransform == null)
            {
                Debug.LogError("EcsCombatBootstrap: playerTransform is not assigned.");
                enabled = false;
                return;
            }

            EnsureHudAuthoring();

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
            world.AddSystem(new WeaponInventorySystem());
            world.AddSystem(new WeaponPickupSystem());
            world.AddSystem(new WeaponShootSystem());
            world.AddSystem(new ShotgunShootSystem());
            world.AddSystem(new LaserBeamSystem());
            world.AddSystem(new MeleeAttackSystem());
            world.AddSystem(new ProjectileLifetimeSystem());
            world.AddSystem(new BeamLifetimeSystem());
            world.AddSystem(new RecoilApplySystem());

            // Rendering — Update
            world.AddSystem(new WeaponViewSystem());
            world.AddSystem(new MuzzleFlashAnimationSystem());
            world.AddSystem(new BeamAnimationSystem());
            world.AddSystem(new CharacterAnimationSystem());
            world.AddSystem(new GroundWeaponHintSystem());
            world.AddSystem(new PlayerHudSystem());

            // Simulation — FixedUpdate
            world.AddSystem(new RecoilDecaySystem());
            world.AddSystem(new PlayerMovementSystem());
            world.AddSystem(new ProjectileMovementSystem());
        }

        // -----------------------------------------------------------------------
        // Player entity creation
        // -----------------------------------------------------------------------

        private void CreatePlayerEntity(EcsWorld world)
        {
            PlayerHudViewComponent hudView = default;
            bool hasHudView = hudAuthoring != null && hudAuthoring.TryGetView(out hudView);

            // Build the exact archetype signature upfront — entity is born in the right archetype.
            ComponentSignature sig = ComponentSignature.Empty
                .With<PlayerTagComponent>()
                .With<TransformComponent>()
                .With<InputStateComponent>()
                .With<AimComponent>()
                .With<MovementComponent>()
                .With<HealthComponent>()
                .With<WeaponLoadoutComponent>()
                .With<EquippedWeaponComponent>()
                .With<WeaponCooldownComponent>()
                .With<WeaponViewComponent>()
                .With<RecoilComponent>();

            if (hasHudView)
            {
                sig = sig.With<PlayerHudViewComponent>();
            }

            Rigidbody2D rb = playerTransform.GetComponent<Rigidbody2D>();
            if (rb != null) sig = sig.With<RigidbodyComponent>();
            else Debug.LogError("EcsCombatBootstrap: Rigidbody2D required on playerTransform.");

            IsometricCharacterRenderer isoRenderer =
                playerTransform.GetComponentInChildren<IsometricCharacterRenderer>();
            if (isoRenderer != null) sig = sig.With<CharacterRenderComponent>();

            // Include weapon-type tag components in birth signature so the player lands
            // in the correct archetype from the very first frame.
            if (startingWeapon != null)
            {
                switch (startingWeapon.Kind)
                {
                    case WeaponKind.Shotgun:
                        sig = sig.With<ShotgunTagComponent>().With<ShotgunDataComponent>();
                        break;
                    case WeaponKind.Laser:
                        sig = sig.With<LaserTagComponent>().With<LaserDataComponent>();
                        break;
                    case WeaponKind.Melee:
                        sig = sig.With<MeleeTagComponent>().With<MeleeDataComponent>();
                        break;
                }
            }

            EntityId player = world.CreateEntity(sig);

            // Fill component data by ref — direct chunk access, no boxing in the hot path.
            world.GetComponent<TransformComponent>(player).Transform = playerTransform;
            world.GetComponent<AimComponent>(player).Direction = Vector2.right;

            ref MovementComponent movement = ref world.GetComponent<MovementComponent>(player);
            PlayerAuthoring authoring = playerTransform.GetComponent<PlayerAuthoring>();
            float maxHealth = 100f;
            if (authoring != null)
            {
                movement.BaseSpeed = authoring.BaseMovementSpeed;
                movement.UseIsometricAxes = authoring.UseIsometricAxes;
                movement.IsometricRightAxis = authoring.IsometricRightAxis;
                movement.IsometricUpAxis = authoring.IsometricUpAxis;
                maxHealth = authoring.MaxHealth;
            }
            else
            {
                Debug.LogWarning("EcsCombatBootstrap: PlayerAuthoring not found on playerTransform. Using defaults.");
                movement.BaseSpeed = 5f;
                movement.UseIsometricAxes = true;
                movement.IsometricRightAxis = new Vector2(1f, 0.5f);
                movement.IsometricUpAxis = new Vector2(-1f, 0.5f);
            }

            ref HealthComponent health = ref world.GetComponent<HealthComponent>(player);
            health.Max = maxHealth;
            health.Current = maxHealth;

            if (hasHudView)
            {
                world.GetComponent<PlayerHudViewComponent>(player) = hudView;
            }

            ref WeaponLoadoutComponent loadout = ref world.GetComponent<WeaponLoadoutComponent>(player);
            loadout.ActiveIndex = 0;

            if (startingWeapon != null)
            {
                loadout.Slot0 = WeaponDefinitionToSlot(startingWeapon);
                WeaponInventoryUtility.ApplySlotToEntity(world, player, ref loadout, 0);
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
                FindObjectsByType<GroundWeaponAuthoring>(FindObjectsInactive.Exclude);
            if (authorings == null || authorings.Length == 0)
            {
                return;
            }

            ComponentSignature signature = ComponentSignature.Empty
                .With<GroundWeaponTagComponent>()
                .With<TransformComponent>()
                .With<WeaponComponent>()
                .With<GroundWeaponHintViewComponent>();

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
                weapon.DisplayName = authoring.Definition.DisplayName;
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
                weapon.ShootEffectFrames = authoring.Definition.ShootEffectFrames;
                weapon.ShootEffectFrameDuration = authoring.Definition.ShootEffectFrameDuration > 0f
                    ? authoring.Definition.ShootEffectFrameDuration : 0.05f;
                weapon.ShootEffectScale = authoring.Definition.ShootEffectScale > 0f
                    ? authoring.Definition.ShootEffectScale : 1f;
                weapon.ShootEffectMuzzleOffset = authoring.Definition.ShootEffectMuzzleOffset;
                weapon.RecoilStrength = authoring.Definition.RecoilStrength;
                weapon.RecoilDecayRate = authoring.Definition.RecoilDecayRate > 0f
                    ? authoring.Definition.RecoilDecayRate : 8f;
                // Kind and type-specific fields are read by WeaponPickupSystem to populate
                // ShotgunDataComponent / LaserDataComponent / MeleeDataComponent on pickup.
                weapon.Kind          = authoring.Definition.Kind;
                weapon.PelletCount   = authoring.Definition.PelletCount > 0 ? authoring.Definition.PelletCount : 8;
                weapon.SpreadAngle   = authoring.Definition.SpreadAngle;
                weapon.BeamDuration  = authoring.Definition.BeamDuration > 0f ? authoring.Definition.BeamDuration : 0.3f;
                weapon.BeamWidth     = authoring.Definition.BeamWidth > 0f ? authoring.Definition.BeamWidth : 0.1f;
                weapon.BeamColor     = authoring.Definition.BeamColor;
                weapon.MeleeRange    = authoring.Definition.MeleeRange > 0f ? authoring.Definition.MeleeRange : 1.5f;
                weapon.MeleeArcAngle = authoring.Definition.MeleeArcAngle > 0f ? authoring.Definition.MeleeArcAngle : 90f;

                world.GetComponent<GroundWeaponHintViewComponent>(id) =
                    GroundWeaponHintFactory.Create(authoring.transform);
            }
        }

        private static WeaponSlotEntry WeaponDefinitionToSlot(WeaponDefinition definition)
        {
            WeaponComponent weapon = new WeaponComponent
            {
                DisplayName = definition.DisplayName,
                WeaponSprite = definition.WeaponSprite,
                ProjectileSprite = definition.ProjectileSprite,
                WeaponSpriteScale = definition.WeaponSpriteScale,
                ProjectileSpriteScale = definition.ProjectileSpriteScale,
                WeaponSortingOrder = definition.WeaponSortingOrder,
                ProjectileSortingOrder = definition.ProjectileSortingOrder,
                FireRate = definition.FireRate,
                ProjectileSpeed = definition.ProjectileSpeed,
                ProjectileLifetime = definition.ProjectileLifetime,
                MuzzleOffset = definition.MuzzleOffset,
                MovementSpeedMultiplier = definition.MovementSpeedMultiplier,
                ShootEffectFrames = definition.ShootEffectFrames,
                ShootEffectFrameDuration = definition.ShootEffectFrameDuration,
                ShootEffectScale = definition.ShootEffectScale,
                ShootEffectMuzzleOffset = definition.ShootEffectMuzzleOffset,
                RecoilStrength = definition.RecoilStrength,
                RecoilDecayRate = definition.RecoilDecayRate,
                Kind = definition.Kind,
                PelletCount = definition.PelletCount,
                SpreadAngle = definition.SpreadAngle,
                BeamDuration = definition.BeamDuration,
                BeamWidth = definition.BeamWidth,
                BeamColor = definition.BeamColor,
                MeleeRange = definition.MeleeRange,
                MeleeArcAngle = definition.MeleeArcAngle,
            };

            return WeaponInventoryUtility.FromWeaponComponent(in weapon);
        }

        private void DisableLegacyMovementController()
        {
            // Legacy MonoBehaviour kept for reference; ECS owns movement when bootstrap is active.
            IsometricPlayerMovementController legacy =
                playerTransform.GetComponent<IsometricPlayerMovementController>();
            if (legacy != null) legacy.enabled = false;
        }

        private void EnsureHudAuthoring()
        {
            if (hudAuthoring == null)
            {
                hudAuthoring = GetComponent<PlayerHudAuthoring>();
            }

            if (hudAuthoring == null)
            {
                hudAuthoring = gameObject.AddComponent<PlayerHudAuthoring>();
            }

            hudAuthoring.EnsureBuilt();
        }
    }
}
