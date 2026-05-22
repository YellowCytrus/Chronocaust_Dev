# HOWTO: Creating New Entities and Components

## Concepts first

- An **entity** is just a versioned integer (`EntityId`). It has no data itself.
- **Components** are plain `struct`s. Data lives in `ArchetypeChunk` arrays, not on the entity.
- An **archetype** is uniquely defined by the set of component types an entity has. If two entities have the same component set, they share the same archetype and their data is co-located in the same chunk arrays.
- Structural changes (adding/removing a component) **move the entity** from its current archetype to a new one. This is called **migration**.

---

## Step 1 — Define a component

Add a `struct` to `Assets/Scripts/ECS/Components/CombatComponents.cs` (or a new file):

```csharp
// In namespace Chronocaust.Ecs.Components
public struct HealthComponent : IEcsComponent
{
    public float Current;
    public float Max;
}
```

Rules:
- Must be a `struct`, not a `class`.
- Must implement `IEcsComponent` (marker interface, no methods).
- Keep it plain data. No methods, no constructors with logic.

---

## Step 2 — Create an entity in Bootstrap (or a factory)

Build the full `ComponentSignature` first, then call `world.CreateEntity(sig)`. Fill values by ref immediately after.

### Example: Player character

```csharp
ComponentSignature sig = ComponentSignature.Empty
    .With<PlayerTagComponent>()
    .With<TransformComponent>()
    .With<InputStateComponent>()
    .With<AimComponent>()
    .With<MovementComponent>()
    .With<EquippedWeaponComponent>() // Игрок всегда имеет слот под оружие
    .With<WeaponCooldownComponent>()
    .With<WeaponViewComponent>();

EntityId player = world.CreateEntity(sig);

PlayerAuthoring playerAuthoring = playerTransform.GetComponent<PlayerAuthoring>();
if (playerAuthoring == null) Debug.LogWarning("Missing PlayerAuthoring on player.");

world.GetComponent<TransformComponent>(player).Transform = playerTransform;

ref MovementComponent m = ref world.GetComponent<MovementComponent>(player);
if (playerAuthoring != null)
{
    m.BaseSpeed = playerAuthoring.BaseMovementSpeed;
    m.UseIsometricAxes = playerAuthoring.UseIsometricAxes;
    m.IsometricRightAxis = playerAuthoring.IsometricRightAxis;
    m.IsometricUpAxis = playerAuthoring.IsometricUpAxis;
}
else
{
    m.BaseSpeed = 5f; // Default if no authoring
    m.UseIsometricAxes = true;
    m.IsometricRightAxis = new Vector2(1f, 0.5f);
    m.IsometricUpAxis = new Vector2(-1f, 0.5f);
}

world.GetComponent<RigidbodyComponent>(player).Rigidbody = rb; // rb comes from Unity's Rigidbody2D
world.GetComponent<CharacterRenderComponent>(player).Renderer = isoRenderer; // isoRenderer from Unity's IsometricCharacterRenderer

// EquippedWeaponComponent по умолчанию пуст. Если в EcsCombatBootstrap задано startingWeapon,
// его поля будут заполнены из WeaponDefinition. Иначе — игрок стартует без оружия.

world.GetComponent<WeaponCooldownComponent>(player).CooldownRemaining = 0f; // Начальный кулдаун

// WeaponViewComponent инициализируется в EcsCombatBootstrap через CreateWeaponViewObject
// и связывается с GameObject-ом, который отображает оружие.
```

### Example: Enemy character

```csharp
// Enemy has health and movement but no player tag or weapon.
ComponentSignature sig = ComponentSignature.Empty
    .With<TransformComponent>()
    .With<MovementComponent>()
    .With<RigidbodyComponent>()
    .With<HealthComponent>();   // new component added above

EntityId enemy = world.CreateEntity(sig);

world.GetComponent<TransformComponent>(enemy).Transform = enemyTransform;

ref HealthComponent hp = ref world.GetComponent<HealthComponent>(enemy);
hp.Current = 100f;
hp.Max     = 100f;

ref MovementComponent m = ref world.GetComponent<MovementComponent>(enemy);
m.Speed = 3f;
```

Because `enemy` has a different signature than `player`, they live in **separate archetypes**. Systems that query `PlayerTagComponent` will never visit enemy entities.

---

## Step 3 — Write a system

Create a file in `Assets/Scripts/ECS/Systems/`. Implement `IEcsUpdateSystem` or `IEcsFixedUpdateSystem`.

### Example: EnemyHealthSystem

```csharp
public sealed class EnemyHealthSystem : IEcsUpdateSystem
{
    // Cached query — initialised once, auto-updates when new archetypes are registered.
    private EcsQuery<HealthComponent, TransformComponent> _query;

    public void Update(EcsWorld world, float deltaTime)
    {
        _query ??= world.CreateQuery<HealthComponent, TransformComponent>();

        _query.ForEach((EntityId id, ref HealthComponent hp, ref TransformComponent transform) =>
        {
            // Direct array access — no Has(), no TryGet(), guaranteed presence.
            if (hp.Current <= 0f)
            {
                world.CommandBuffer.DestroyEntity(id); // queued, applied after ForEach
            }
        });
    }
}
```

Register it in `EcsCombatBootstrap.RegisterSystems`:

```csharp
world.AddSystem(new EnemyHealthSystem());
```

---

## Step 4 — Creating Ground Weapons (Pickups)

Instead of configuring a large list in `EcsCombatBootstrap`, each ground weapon is defined directly in the Unity scene using a `GroundWeaponAuthoring` MonoBehaviour.

1. **Create WeaponDefinition Assets:**
   - In your Project window, right-click -> `Create -> Chronocaust -> ECS -> Weapon Definition`.
   - Name it (e.g., `WD_Pistol`, `WD_Shotgun`).
   - Fill in **Visuals** (sprites, scales, sorting, `WeaponVisualBaseRotationDeg`, mirror flags, **`MuzzleOffset`** — muzzle for ranged and **weapon mount** in aim space for melee), then **Stats** (fire rate, projectile speed/lifetime), **Character Modifiers**, and weapon-type sections (**Shotgun** / **Laser** / **Melee** as needed). Melee pivot on the art is configured in the **Sprite Editor** (sprite pivot), not via a separate runtime grip field.

2. **Place a Ground Weapon in the Scene:**
   - Create a new `GameObject` in your scene (e.g., `GroundWeapon_Pistol`).
   - Position it where you want the weapon to appear.
   - Add a `SpriteRenderer` component to this GameObject and assign its sprite (e.g., `WD_Pistol.WeaponSprite`). Configure its `Sorting Layer` and `Order in Layer` as needed for how it appears on the ground.
   - Add the `GroundWeaponAuthoring` component to this GameObject.
   - In the `GroundWeaponAuthoring` component, drag and drop your created `WeaponDefinition` (e.g., `WD_Pistol`) into the `Definition` field.

At runtime, `EcsCombatBootstrap` will find all `GroundWeaponAuthoring` components in the scene and convert them into ECS entities (with `GroundWeaponTagComponent`, `TransformComponent`, and `WeaponComponent`). When a player interacts with it, `WeaponPickupSystem` handles the transfer of data and removal of the GameObject.

---

## Step 5 — Add a component to an existing entity at runtime

Structural changes must go through `CommandBuffer` so they happen outside of active iteration.

### Option A: From Bootstrap or game code outside a system

```csharp
// Immediate (safe outside iteration):
world.AddComponent(entityId, new ShieldComponent { Value = 50f });
// Entity migrates: Archetype[...] → Archetype[...+ShieldComponent]
```

### Option B: From inside a system's ForEach

```csharp
// WRONG — modifies archetype during iteration:
world.AddComponent(id, new ShieldComponent { Value = 50f }); // DO NOT DO THIS

// CORRECT — defer via CommandBuffer:
world.CommandBuffer.AddComponent(id, new ShieldComponent { Value = 50f });
// Applied during Playback() at the end of the frame.
```

### Option C: Remove a component at runtime

```csharp
// From inside ForEach — always deferred:
world.CommandBuffer.RemoveComponent<ShieldComponent>(id);
// Entity migrates: Archetype[...+Shield] → Archetype[...-Shield]
```

---

## Why you must NOT do structural changes inside ForEach

`ForEach` iterates `ArchetypeChunk` arrays directly. If you add or remove a component mid-iteration:

1. The entity migrates to a different archetype (different chunk).
2. The original chunk uses **swap-back** to fill the gap — the last entity in the chunk moves to the freed slot.
3. The loop index is now pointing at a different entity or past the end of the array.

This causes skipped entities, double-processing, or out-of-bounds access.

**The `CommandBuffer` exists to solve this**: all ops are queued and applied in `Playback()` after all `ForEach` calls for the frame have finished.

---

## Quick reference

```
Define component   → struct T : IEcsComponent in CombatComponents.cs
Create entity      → world.CreateEntity(signature) in Bootstrap
Fill values        → ref T c = ref world.GetComponent<T>(id); c.Field = value;
Write a system     → IEcsUpdateSystem / IEcsFixedUpdateSystem; cache EcsQuery<...>
Iterate            → _query.ForEach((id, ref T1, ref T2) => { ... })
Deferred ops       → world.CommandBuffer.AddComponent / RemoveComponent / DestroyEntity
Destroy            → world.CommandBuffer.DestroyEntity(id) (queued; safe inside ForEach)
Register system    → world.AddSystem(new MySystem()) in EcsCombatBootstrap
Create Ground Weapon → Place GameObject with GroundWeaponAuthoring and WeaponDefinition in scene
Define Weapon Data   → Create ScriptableObject WeaponDefinition
```
