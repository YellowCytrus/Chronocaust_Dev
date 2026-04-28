# Chronocaust ECS — Architecture Reference

## Overview

This is a hand-rolled **archetype ECS** for Unity. It follows the data-oriented design rules in `.cursorrules`.

```
ComponentSignature (bitmask)
        │
        ▼
ArchetypeRegistry ──► Archetype ──► ArchetypeChunk[]
        │                                │
        │             EntityId[]  T1[]  T2[] ... TN[]  (SoA, cache-friendly)
        │
EcsWorld
  ├─ EntityPool         (versioned EntityId, O(1) create/recycle)
  ├─ EntityRecord[]     (per-entity location: Archetype + ChunkIndex + Row)
  ├─ CommandBuffer      (deferred structural ops with migration)
  └─ Systems[]          (pure functions over archetype data)

EcsQuery<T1..TN>
  └─ List<Archetype>   (precomputed at query creation; auto-updated on new archetype)
```

### Key rules enforced by design

| Rule | How it is enforced |
|---|---|
| No `Has()` in iteration | `EcsQuery` only visits archetypes guaranteed to have all required types |
| No entity-centric loops | `ForEach` iterates `chunk.Entities[]` arrays, not an entity list |
| No per-component global storage | Data lives only inside `ArchetypeChunk` arrays |
| Structural changes via CommandBuffer | `AddComponent`/`RemoveComponent` queue migration ops; `Playback` applies after iteration |
| No `Time.time` in systems | `WeaponCooldownComponent.CooldownRemaining` is decremented by `deltaTime` |
| Simulation / rendering split | FixedUpdate systems never call rendering methods; `CharacterAnimationSystem` is Update-only |
| Core independent of Unity types | `EcsWorld`, `Archetype`, `ArchetypeChunk`, `EcsQuery` have zero Unity imports |

---

## Core files

| File | Role |
|---|---|
| `Core/ComponentTypeId.cs` | Static generic registry — assigns a stable `int` to each component type |
| `Core/ComponentSignature.cs` | `ulong` bitmask of type IDs; immutable; used as archetype key |
| `Core/ArchetypeChunk.cs` | One contiguous SoA block: `EntityId[]` + `T[]` per type in signature |
| `Core/Archetype.cs` | Owns `List<ArchetypeChunk>`; handles add/remove/copy |
| `Core/ArchetypeRegistry.cs` | `ComponentSignature → Archetype` map; fires `OnArchetypeCreated` |
| `Core/EntityRecord.cs` | `(Archetype, ChunkIndex, Row)` per entity — location index |
| `Core/EntityPool.cs` | Versioned slot allocator; detects stale `EntityId` via generation |
| `Core/EcsWorld.cs` | Coordinator: create entity, get component, migrate, update loop |
| `Core/EcsQuery.cs` | Precomputed archetype list; `ForEach` iterates chunk arrays |
| `Core/CommandBuffer.cs` | Deferred ops queue; migration-aware `Playback` |
| `Core/IEcsComponent.cs` | Marker interface for all component structs |
| `Core/IEcsSystems.cs` | `IEcsUpdateSystem` / `IEcsFixedUpdateSystem` interfaces |

---

## Components (`Components/CombatComponents.cs`)

All components are `struct`. Fields with Unity object references (Transform, Rigidbody2D, etc.) are managed structs — valid in C# without Burst.

| Component | Purpose |
|---|---|
| `PlayerTagComponent` | Archetype filter: marks a player entity |
| `TransformComponent` | Unity `Transform` reference |
| `InputStateComponent` | Raw input snapshot (mouse position, fire button, WASD) |
| `AimComponent` | Normalised aim direction (world space) |
| `MovementComponent` | Speed, iso axes, `LastDirection` (written by movement, read by animation) |
| `WeaponComponent` | Weapon config (sprites, fire rate, projectile stats, muzzle offset) |
| `WeaponCooldownComponent` | `CooldownRemaining` — relative timer, no `Time.time` |
| `WeaponViewComponent` | Transform + SpriteRenderer of the visual weapon child object |
| `RigidbodyComponent` | Unity `Rigidbody2D` reference |
| `CharacterRenderComponent` | `IsometricCharacterRenderer` reference (View layer) |
| `ProjectileComponent` | Direction, speed, `TimeLeft` |

---

## Systems

### Update systems (called every frame)

| System | Reads | Writes | Notes |
|---|---|---|---|
| `PlayerInputSystem` | `PlayerTag`, `TransformComponent` | `InputStateComponent` | Samples `Input.*` once per frame |
| `PlayerAimSystem` | `PlayerTag`, `TransformComponent`, `InputStateComponent` | `AimComponent` | Normalised mouse direction |
| `WeaponShootSystem` | `PlayerTag`, `Transform`, `Input`, `Aim`, `Weapon`, `WeaponCooldown` | `WeaponCooldown.CooldownRemaining`; queues projectile spawn | Pre-allocated list, no per-frame alloc |
| `ProjectileLifetimeSystem` | `ProjectileComponent`, `TransformComponent` | `ProjectileComponent.TimeLeft`; queues `DestroyEntity` | |
| `WeaponViewSystem` | `TransformComponent`, `AimComponent`, `WeaponComponent`, `WeaponViewComponent` | Unity transforms + sprite | Rendering layer |
| `CharacterAnimationSystem` | `MovementComponent.LastDirection`, `CharacterRenderComponent` | Animator state | Rendering layer; reads data written by FixedUpdate |

### FixedUpdate systems (called at fixed physics rate)

| System | Reads | Writes | Notes |
|---|---|---|---|
| `PlayerMovementSystem` | `PlayerTag`, `Input`, `Movement`, `RigidbodyComponent` | `Rigidbody2D.position`, `Movement.LastDirection` | No rendering calls |
| `ProjectileMovementSystem` | `ProjectileComponent`, `TransformComponent` | `Transform.position` | |

---

## System execution order

```
Update()
  1. PlayerInputSystem
  2. PlayerAimSystem
  3. WeaponShootSystem
  4. ProjectileLifetimeSystem
  5. WeaponViewSystem          ← rendering
  6. CharacterAnimationSystem  ← rendering
  [CommandBuffer.Playback + DestroyQueue flush]

FixedUpdate()
  1. PlayerMovementSystem
  2. ProjectileMovementSystem
  [CommandBuffer.Playback + DestroyQueue flush]
```

---

## Rules for writing systems

1. **No `Has()` in iteration** — `EcsQuery<T...>` guarantees component presence by archetype.
2. **No `new GameObject()`** — Unity object creation belongs in Bootstrap or `CommandBuffer.SpawnProjectileImmediate`.
3. **No `Time.time`** — use `deltaTime` parameter or relative component fields.
4. **No structural changes inside `ForEach`** — queue via `world.CommandBuffer` or `world.DestroyEntity`; applied after iteration ends.
5. **No rendering in FixedUpdate systems** — only `Update` systems may call `Renderer.*`, `Animator.*`, etc.
6. **Cache queries** — store `EcsQuery<...>` as a field, initialise with `??=` on first call. Never recreate per frame.
