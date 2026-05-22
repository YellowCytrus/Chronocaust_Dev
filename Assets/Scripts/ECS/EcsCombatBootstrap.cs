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

        [Header("Combat")]
        [Tooltip("Layers that receive projectile and melee damage (e.g. Enemy).")]
        [SerializeField] private LayerMask damageableLayers = ~0;

        private EcsWorld _world;
        private PhysicsEntityRegistry _physicsRegistry;

        private void Awake()
        {
            if (playerTransform == null)
            {
                Debug.LogError("EcsCombatBootstrap: playerTransform is not assigned.");
                enabled = false;
                return;
            }

            _world = new EcsWorld();
            _physicsRegistry = new PhysicsEntityRegistry();
            BindDestroyCallback(_world, _physicsRegistry);
            RegisterSystems(_world, _physicsRegistry);
            CreatePlayerEntity(_world);
            CreateGroundWeapons(_world);
            CreateEnemyEntities(_world, _physicsRegistry);
            DisableLegacyMovementController();
        }

        private void Update() => _world?.Update(Time.deltaTime);
        private void FixedUpdate() => _world?.FixedUpdate(Time.fixedDeltaTime);

        // -----------------------------------------------------------------------
        // Destroy callback — keeps EcsWorld Core free of Unity types
        // -----------------------------------------------------------------------

        private static void BindDestroyCallback(EcsWorld world, PhysicsEntityRegistry registry)
        {
            world.OnEntityDestroyed += id =>
            {
                registry.UnregisterEntity(id);

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

        private void RegisterSystems(EcsWorld world, PhysicsEntityRegistry registry)
        {
            // Simulation — Update
            world.AddSystem(new PlayerInputSystem());
            world.AddSystem(new PlayerAimSystem());
            world.AddSystem(new WeaponPickupSystem());
            world.AddSystem(new WeaponShootSystem());
            world.AddSystem(new ShotgunShootSystem());
            world.AddSystem(new LaserBeamSystem(registry));
            world.AddSystem(new MeleeAttackSystem(registry));
            world.AddSystem(new EnemyAttackSystem());
            world.AddSystem(new EnemyDeathSystem());
            world.AddSystem(new ProjectileLifetimeSystem());
            world.AddSystem(new BeamLifetimeSystem());
            world.AddSystem(new RecoilApplySystem());

            // Rendering — Update
            world.AddSystem(new WeaponViewSystem());
            world.AddSystem(new MuzzleFlashAnimationSystem());
            world.AddSystem(new BeamAnimationSystem());
            world.AddSystem(new CharacterAnimationSystem());

            // Simulation — FixedUpdate
            world.AddSystem(new RecoilDecaySystem());
            world.AddSystem(new PlayerMovementSystem());
            world.AddSystem(new EnemyChaseSystem());
            world.AddSystem(new ProjectileHitSystem(registry, damageableLayers));
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
                .With<HealthComponent>()
                .With<EquippedWeaponComponent>()
                .With<WeaponCooldownComponent>()
                .With<WeaponViewComponent>()
                .With<RecoilComponent>();

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
            if (authoring != null)
            {
                ref HealthComponent health = ref world.GetComponent<HealthComponent>(player);
                health.Max = authoring.MaxHealth > 0f ? authoring.MaxHealth : 100f;
                health.Current = health.Max;
                movement.BaseSpeed = authoring.BaseMovementSpeed;
                movement.UseIsometricAxes = authoring.UseIsometricAxes;
                movement.IsometricRightAxis = authoring.IsometricRightAxis;
                movement.IsometricUpAxis = authoring.IsometricUpAxis;
            }
            else
            {
                Debug.LogWarning("EcsCombatBootstrap: PlayerAuthoring not found on playerTransform. Using defaults.");
                ref HealthComponent health = ref world.GetComponent<HealthComponent>(player);
                health.Max = 100f;
                health.Current = 100f;
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
                equipped.Damage = startingWeapon.Damage;
                equipped.WeaponSpriteScale = startingWeapon.WeaponSpriteScale;
                equipped.ProjectileSpriteScale = startingWeapon.ProjectileSpriteScale;
                equipped.WeaponSortingOrder = startingWeapon.WeaponSortingOrder;
                equipped.ProjectileSortingOrder = startingWeapon.ProjectileSortingOrder;
                equipped.FireRate = startingWeapon.FireRate;
                equipped.ProjectileSpeed = startingWeapon.ProjectileSpeed;
                equipped.ProjectileLifetime = startingWeapon.ProjectileLifetime;
                equipped.MuzzleOffset = startingWeapon.MuzzleOffset;
                equipped.WeaponVisualBaseRotationDeg = startingWeapon.WeaponVisualBaseRotationDeg;
                equipped.WeaponVisualMirrorX = startingWeapon.WeaponVisualMirrorX;
                equipped.WeaponVisualMirrorY = startingWeapon.WeaponVisualMirrorY;
                equipped.MovementSpeedMultiplier = startingWeapon.MovementSpeedMultiplier;
                equipped.ShootEffectFrames = startingWeapon.ShootEffectFrames;
                equipped.ShootEffectFrameDuration = startingWeapon.ShootEffectFrameDuration > 0f
                    ? startingWeapon.ShootEffectFrameDuration : 0.05f;
                equipped.ShootEffectScale = startingWeapon.ShootEffectScale > 0f
                    ? startingWeapon.ShootEffectScale : 1f;
                equipped.ShootEffectMuzzleOffset = startingWeapon.ShootEffectMuzzleOffset;
                equipped.RecoilStrength = startingWeapon.RecoilStrength;
                equipped.RecoilDecayRate = startingWeapon.RecoilDecayRate > 0f
                    ? startingWeapon.RecoilDecayRate : 8f;

                // Fill type-specific data components — they were added to sig above.
                switch (startingWeapon.Kind)
                {
                    case WeaponKind.Shotgun:
                        world.GetComponent<ShotgunDataComponent>(player) = new ShotgunDataComponent
                        {
                            PelletCount = startingWeapon.PelletCount > 0 ? startingWeapon.PelletCount : 8,
                            SpreadAngle = startingWeapon.SpreadAngle
                        };
                        break;
                    case WeaponKind.Laser:
                        world.GetComponent<LaserDataComponent>(player) = new LaserDataComponent
                        {
                            BeamDuration = startingWeapon.BeamDuration > 0f ? startingWeapon.BeamDuration : 0.3f,
                            BeamWidth    = startingWeapon.BeamWidth > 0f ? startingWeapon.BeamWidth : 0.1f,
                            BeamColor    = startingWeapon.BeamColor
                        };
                        break;
                    case WeaponKind.Melee:
                        world.GetComponent<MeleeDataComponent>(player) = MeleeDataSetup.FromWeaponDefinition(startingWeapon);
                        break;
                }
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
                weapon.Damage = authoring.Definition.Damage;
                weapon.WeaponSpriteScale = authoring.Definition.WeaponSpriteScale;
                weapon.ProjectileSpriteScale = authoring.Definition.ProjectileSpriteScale;
                weapon.WeaponSortingOrder = authoring.Definition.WeaponSortingOrder;
                weapon.ProjectileSortingOrder = authoring.Definition.ProjectileSortingOrder;
                weapon.FireRate = authoring.Definition.FireRate;
                weapon.ProjectileSpeed = authoring.Definition.ProjectileSpeed;
                weapon.ProjectileLifetime = authoring.Definition.ProjectileLifetime;
                weapon.MuzzleOffset = authoring.Definition.MuzzleOffset;
                weapon.WeaponVisualBaseRotationDeg = authoring.Definition.WeaponVisualBaseRotationDeg;
                weapon.WeaponVisualMirrorX = authoring.Definition.WeaponVisualMirrorX;
                weapon.WeaponVisualMirrorY = authoring.Definition.WeaponVisualMirrorY;
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
                weapon.MeleeMotionType = authoring.Definition.MeleeMotionType;
                weapon.MeleeStartupDuration = authoring.Definition.MeleeStartupDuration;
                weapon.MeleeActiveDuration = authoring.Definition.MeleeActiveDuration;
                weapon.MeleeRecoveryDuration = authoring.Definition.MeleeRecoveryDuration;
                weapon.MeleeHitWindowStartT = authoring.Definition.MeleeHitWindowStartT;
                weapon.MeleeHitWindowEndT = authoring.Definition.MeleeHitWindowEndT;
                weapon.MeleeAnticipationPull = authoring.Definition.MeleeAnticipationPull;
                weapon.MeleeThrustDistance = authoring.Definition.MeleeThrustDistance;
                weapon.MeleeThrustHitRadius = authoring.Definition.MeleeThrustHitRadius;
                weapon.MeleeThrustVisualTiltMaxDeg = authoring.Definition.MeleeThrustVisualTiltMaxDeg;
                weapon.MeleeSlamWindupDeg = authoring.Definition.MeleeSlamWindupDeg;
                weapon.MeleeSlamDownDeg = authoring.Definition.MeleeSlamDownDeg;
                weapon.MeleeSlamWindupOffsetY = authoring.Definition.MeleeSlamWindupOffsetY;
                weapon.MeleeSlamStrikeDepth = authoring.Definition.MeleeSlamStrikeDepth;
                weapon.MeleeSlamUseFixedAimDir = authoring.Definition.MeleeSlamUseFixedAimDir;
                weapon.MeleeSlamPoseOffsetRight = authoring.Definition.MeleeSlamPoseOffsetRight;
                weapon.MeleeSlamPoseRotRight = authoring.Definition.MeleeSlamPoseRotRight;
                weapon.MeleeSlamPoseOffsetLeft = authoring.Definition.MeleeSlamPoseOffsetLeft;
                weapon.MeleeSlamPoseRotLeft = authoring.Definition.MeleeSlamPoseRotLeft;
                weapon.MeleeSlamShootEffectOffsetRight = authoring.Definition.MeleeSlamShootEffectOffsetRight;
                weapon.MeleeSlamShootEffectOffsetLeft = authoring.Definition.MeleeSlamShootEffectOffsetLeft;
                weapon.MeleeSlamHitSideOffset = authoring.Definition.MeleeSlamHitSideOffset;
                weapon.MeleeSpinTurns = authoring.Definition.MeleeSpinTurns;
                weapon.MeleeViewSuppressFlipY = authoring.Definition.MeleeViewSuppressFlipY;
                weapon.MeleeIdleVisualAimSmoothHz = authoring.Definition.MeleeIdleVisualAimSmoothHz;
            }
        }

        private static void CreateEnemyEntities(EcsWorld world, PhysicsEntityRegistry registry)
        {
            EnemyAuthoring[] authorings = FindObjectsByType<EnemyAuthoring>(FindObjectsSortMode.None);
            if (authorings == null || authorings.Length == 0)
            {
                return;
            }

            int count = authorings.Length;
            for (int i = 0; i < count; i++)
            {
                EnemyAuthoring authoring = authorings[i];
                if (authoring == null) continue;

                ComponentSignature sig = ComponentSignature.Empty
                    .With<EnemyTagComponent>()
                    .With<TransformComponent>()
                    .With<AimComponent>()
                    .With<MovementComponent>()
                    .With<HealthComponent>()
                    .With<EnemyChaseComponent>()
                    .With<EnemyAttackComponent>()
                    .With<EquippedWeaponComponent>()
                    .With<WeaponCooldownComponent>()
                    .With<WeaponViewComponent>();

                Rigidbody2D rb = authoring.GetComponent<Rigidbody2D>();
                if (rb != null) sig = sig.With<RigidbodyComponent>();
                else Debug.LogWarning($"EcsCombatBootstrap: Enemy '{authoring.name}' has no Rigidbody2D.");

                IsometricCharacterRenderer isoRenderer =
                    authoring.GetComponentInChildren<IsometricCharacterRenderer>();
                if (isoRenderer != null) sig = sig.With<CharacterRenderComponent>();

                EntityId enemy = world.CreateEntity(sig);
                world.GetComponent<TransformComponent>(enemy).Transform = authoring.transform;
                world.GetComponent<AimComponent>(enemy).Direction = Vector2.right;

                ref MovementComponent movement = ref world.GetComponent<MovementComponent>(enemy);
                movement.BaseSpeed = authoring.MoveSpeed > 0f ? authoring.MoveSpeed : 3.5f;
                movement.UseIsometricAxes = false;
                movement.IsometricRightAxis = Vector2.right;
                movement.IsometricUpAxis = Vector2.up;

                ref HealthComponent health = ref world.GetComponent<HealthComponent>(enemy);
                health.Max = authoring.MaxHealth > 0f ? authoring.MaxHealth : 40f;
                health.Current = health.Max;

                ref EnemyChaseComponent chase = ref world.GetComponent<EnemyChaseComponent>(enemy);
                chase.MoveSpeed = authoring.MoveSpeed > 0f ? authoring.MoveSpeed : 3.5f;
                chase.StopDistance = authoring.StopDistance > 0f ? authoring.StopDistance : 0.85f;

                ref EnemyAttackComponent attack = ref world.GetComponent<EnemyAttackComponent>(enemy);
                attack.AttackRange = authoring.AttackRange > 0f ? authoring.AttackRange : 1.2f;
                attack.UnarmedDamage = authoring.UnarmedDamage > 0f ? authoring.UnarmedDamage : 5f;

                if (authoring.StartingWeapon != null)
                {
                    ref EquippedWeaponComponent equipped = ref world.GetComponent<EquippedWeaponComponent>(enemy);
                    equipped.WeaponSprite = authoring.StartingWeapon.WeaponSprite;
                    equipped.ProjectileSprite = authoring.StartingWeapon.ProjectileSprite;
                    equipped.Damage = authoring.StartingWeapon.Damage;
                    equipped.WeaponSpriteScale = authoring.StartingWeapon.WeaponSpriteScale;
                    equipped.ProjectileSpriteScale = authoring.StartingWeapon.ProjectileSpriteScale;
                    equipped.WeaponSortingOrder = authoring.StartingWeapon.WeaponSortingOrder;
                    equipped.ProjectileSortingOrder = authoring.StartingWeapon.ProjectileSortingOrder;
                    equipped.FireRate = authoring.StartingWeapon.FireRate;
                    equipped.ProjectileSpeed = authoring.StartingWeapon.ProjectileSpeed;
                    equipped.ProjectileLifetime = authoring.StartingWeapon.ProjectileLifetime;
                    equipped.MuzzleOffset = authoring.StartingWeapon.MuzzleOffset;
                    equipped.WeaponVisualBaseRotationDeg = authoring.StartingWeapon.WeaponVisualBaseRotationDeg;
                    equipped.WeaponVisualMirrorX = authoring.StartingWeapon.WeaponVisualMirrorX;
                    equipped.WeaponVisualMirrorY = authoring.StartingWeapon.WeaponVisualMirrorY;
                    equipped.MovementSpeedMultiplier = authoring.StartingWeapon.MovementSpeedMultiplier;
                    equipped.ShootEffectFrames = authoring.StartingWeapon.ShootEffectFrames;
                    equipped.ShootEffectFrameDuration = authoring.StartingWeapon.ShootEffectFrameDuration > 0f
                        ? authoring.StartingWeapon.ShootEffectFrameDuration : 0.05f;
                    equipped.ShootEffectScale = authoring.StartingWeapon.ShootEffectScale > 0f
                        ? authoring.StartingWeapon.ShootEffectScale : 1f;
                    equipped.ShootEffectMuzzleOffset = authoring.StartingWeapon.ShootEffectMuzzleOffset;
                    equipped.RecoilStrength = authoring.StartingWeapon.RecoilStrength;
                    equipped.RecoilDecayRate = authoring.StartingWeapon.RecoilDecayRate > 0f
                        ? authoring.StartingWeapon.RecoilDecayRate : 8f;
                }

                WeaponViewComponent view = CreateWeaponViewObject(authoring.transform);
                world.GetComponent<WeaponViewComponent>(enemy) = view;

                if (rb != null)
                {
                    world.GetComponent<RigidbodyComponent>(enemy).Rigidbody = rb;
                }

                if (isoRenderer != null)
                {
                    world.GetComponent<CharacterRenderComponent>(enemy).Renderer = isoRenderer;
                }

                registry.RegisterDamageableEntity(enemy, authoring.gameObject);
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
