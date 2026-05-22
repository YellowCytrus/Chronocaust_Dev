# Chronocaust_Dev — Architecture & Implementation Audit

**Document type:** implementation-driven technical audit  
**Primary sources:** repository source (`Assets/Scripts/ECS/**`, bootstrap, `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`), not marketing copy  
**Secondary:** existing `docs/**` (validated; contradictions noted explicitly)

---

## 1. Executive summary

Chronocaust_Dev is a **Unity 6** 2D project using a **custom, hand-rolled archetype ECS** for combat-related simulation and presentation hooks. Unity remains the host: scenes, prefabs, physics (`Rigidbody2D`), input, rendering, and asset pipeline. The ECS layer (`Chronocaust.Ecs.Core`) is **chunk-based SoA**, **query-driven**, and **structural-change deferred** via `CommandBuffer` + end-of-tick flush in `EcsWorld`.

**Strengths (evidence-based):**

- Clear separation between **Update** (input, aim, weapons, some physics impulses, rendering-oriented systems) and **FixedUpdate** (movement integration, projectile integration, recoil decay).
- **No `Time.time` in combat cooldown path** — `WeaponCooldownComponent` uses `deltaTime` (see `CombatComponents.cs` tooltip and systems).
- **Deterministic system ordering** declared in one place: `EcsCombatBootstrap.RegisterSystems`.
- **Weapon type extension** via mutually exclusive tag components (`ShotgunTagComponent`, `LaserTagComponent`, `MeleeTagComponent`) + paired data structs; default pistol path uses query exclusions.
- **Melee** modeled as phased attack + pose generation `f(t)` + hit windows; **CommandBuffer** supports shot events for decoupled listeners.

**Material gaps / risks:**

- **No automated tests** under `Tests/` (glob search: zero `.cs` test files).
- **No CI workflows** in `.github/` (repository search: none).
- **Heavy reliance on Unity globals** in hot paths: `Input.*`, `Camera.main`, `Physics2D` (documented as coupling).
- **Documentation drift:** several `docs/flows/*.md` and `docs/models/*.md` files describe an older, narrower system set and incorrect Unity version.

---

## 2. Inspection method & certainty

| Area | Method | Certainty |
|------|--------|-----------|
| Unity editor version | `ProjectSettings/ProjectVersion.txt` | **Confirmed** (`6000.4.5f1`) |
| Packages | `Packages/manifest.json` | **Confirmed** |
| ECS execution order | `EcsCombatBootstrap.RegisterSystems` | **Confirmed** |
| Flush / migration | `EcsWorld.Update` / `FixedUpdate` + `FlushPendingOps` | **Confirmed** |
| CI/CD | `.github/**` glob | **Absent** (confirmed empty) |
| Unit/integration tests | `**/Tests/**/*.cs` glob | **Absent** |
| Full `Assets/` art/audio inventory | Not exhaustively classified | **Out of scope** for this audit |

**Inference** (labeled as such): production deployment process is **unknown** beyond generic Unity build; no Dockerfile or k8s manifests found in repo root search.

---

## 3. Technology stack

| Layer | Evidence |
|-------|----------|
| Engine | Unity **6000.4.5f1** (`ProjectSettings/ProjectVersion.txt`) |
| Language | C# (Unity scripting) |
| 2D | `manifest.json`: `com.unity.2d.sprite`, `2d.tilemap`, `2d.pixel-perfect`; Physics2D usage in systems |
| ECS | **Custom** in `Assets/Scripts/ECS/Core/*` — not Unity.Entities/DOTS |
| Optional services in manifest | Ads, Analytics, Purchasing, Collab — **present as dependencies**; **usage not audited** file-by-file |

---

## 4. Repository structure (architecturally relevant)

```
Assets/Scripts/ECS/          # Game ECS: Core + Components + Systems + authoring SOs
Assets/Scripts/              # MonoBehaviour glue (camera, character renderer, legacy movement)
Assets/_External/            # Unity 2D tech demos / editor brushes — third-party, not game ECS
docs/                        # Human docs (partially stale vs code — see §11)
ProjectSettings/             # Unity project config
Packages/manifest.json       # UPM lock-in
```

**Boundary:** `Assets/_External/**` is **vendor/demo** code; game logic audit focuses on `Assets/Scripts/ECS` and bootstrap-adjacent scripts.

---

## 5. High-level architecture

### 5.1 Hybrid model

1. **Unity lifecycle** owns the process: `EcsCombatBootstrap` (`MonoBehaviour`) creates `EcsWorld` in `Awake`, drives `Update` / `FixedUpdate`.
2. **ECS world** owns entity/component storage and system dispatch.
3. **Unity objects** are referenced from components (`Transform`, `Rigidbody2D`, `SpriteRenderer`, `LineRenderer`) — acceptable tradeoff for small-scale 2D; **not** pure DOTS.

### 5.2 Layering (actual)

| Layer | Examples | Runs in |
|-------|----------|---------|
| Input / aim | `PlayerInputSystem`, `PlayerAimSystem` | Update |
| Simulation (gameplay) | `WeaponShootSystem`, `ShotgunShootSystem`, `LaserBeamSystem`, `MeleeAttackSystem`, `ProjectileLifetimeSystem`, `BeamLifetimeSystem`, `RecoilApplySystem`, `WeaponPickupSystem` | Update |
| Physics integration | `PlayerMovementSystem`, `ProjectileMovementSystem`, `RecoilDecaySystem` | FixedUpdate |
| Presentation | `WeaponViewSystem`, `MuzzleFlashAnimationSystem`, `BeamAnimationSystem`, `CharacterAnimationSystem` | Update |

**Rule in code comments/README:** FixedUpdate systems must not drive rendering APIs; several systems are explicitly labeled “Rendering layer”.

---

## 6. ECS core — reconstruction from code

### 6.1 Storage model

- **Archetype** = unique `ComponentSignature` (bitmask of type IDs).
- **Chunk** = SoA arrays: `EntityId[]` + one array per component type in signature (`ArchetypeChunk`).
- **Entity location** = `(Archetype, ChunkIndex, Row)` in `EntityRecord[]`.
- **EntityId** is versioned (`EntityPool`) to detect stale references after recycle.

### 6.2 Queries

- `EcsQuery<T1…TN>` holds a **precomputed** list of matching archetypes, updated on `OnArchetypeCreated`.
- Iteration is **chunk-first** `ForEach` delegates — no per-entity `Has<T>()` in the hot path.

### 6.3 Structural changes

- `EcsWorld.AddComponent` / `RemoveComponent` / `DestroyEntity` enqueue `MigrationOp`.
- End of `Update` / `FixedUpdate`: `FlushPendingOps` runs migrations, then **`CommandBuffer.Playback`**, then destroys (`OnEntityDestroyed` fires before data removal for hooked cleanup).

### 6.4 CommandBuffer responsibilities

From `CommandBuffer.cs` summary: deferred **Add/Remove/Destroy**, spawn queues for **projectiles**, **muzzle flashes**, **beams**, **`ShotEvent` list** (same-frame, cleared on playback). Spawn paths create Unity objects (implementation uses Unity APIs — core world types stay free of Unity where possible, but `CommandBuffer` itself is Unity-aware).

---

## 7. Runtime lifecycle (authoritative order)

### 7.1 `IEcsUpdateSystem` (registration order in `EcsCombatBootstrap`)

1. `PlayerInputSystem`  
2. `PlayerAimSystem`  
3. `WeaponPickupSystem`  
4. `WeaponShootSystem`  
5. `ShotgunShootSystem`  
6. `LaserBeamSystem`  
7. `MeleeAttackSystem`  
8. `ProjectileLifetimeSystem`  
9. `BeamLifetimeSystem`  
10. `RecoilApplySystem`  
11. `WeaponViewSystem` *(rendering)*  
12. `MuzzleFlashAnimationSystem`  
13. `BeamAnimationSystem`  
14. `CharacterAnimationSystem`  

### 7.2 `IEcsFixedUpdateSystem`

1. `RecoilDecaySystem`  
2. `PlayerMovementSystem`  
3. `ProjectileMovementSystem`  

### 7.3 Post-tick

After each world tick: structural flush + command buffer playback + destroy queue (see `EcsWorld.FlushPendingOps`).

---

## 8. Combat & weapons — domain summary

### 8.1 Weapon kinds

`WeaponKind` enum + `WeaponDefinition` ScriptableObject author most stats. Runtime on player:

- **Default:** `WeaponShootSystem` with query **excluding** shotgun/laser/melee tags.
- **Shotgun / Laser / Melee:** dedicated systems; `WeaponShootSystem` does not run for those archetypes.

### 8.2 Ground weapons & pickup

- `GroundWeaponAuthoring` + `EcsCombatBootstrap.CreateGroundWeapons` spawn entities with `WeaponComponent`.
- `WeaponPickupSystem` uses spatial hashing, **structural changes deferred** outside `ForEach` (remove tags, add tags, `MeleeDataSetup`).

### 8.3 Melee (current implementation)

- **Phases:** `MeleeAttackSystem` drives `Startup` / `Active` / `Recovery` with hit logic only in **Active** window and normalized `t`.
- **Pose:** `MeleeWeaponPose.Evaluate` — pure function of `t` + motion type (arc, thrust, slam, spin).
- **View:** `WeaponViewSystem` — позиция визуала из **`MuzzleOffset`** (+ смещение из `MeleeWeaponPose` в пространстве прицела); поворот по прицелу, `WeaponVisualBaseRotationDeg` и фазе swing; **`WeaponVisualMirrorX` / `WeaponVisualMirrorY`** на `SpriteRenderer`; для idle melee — экспоненциальное сглаживание угла (`IdleVisualSmoothedAimDeg`); во время атаки направление визуала заморожено на `AttackAimDir`; политика `flipY` при `MeleeViewSuppressFlipY`. Внутренняя ось вращения на текстуре — только **pivot спрайта** в редакторе Unity, без дополнительного ECS-поля смещения пивота.

### 8.4 Recoil

- `RecoilApplySystem` (Update) applies impulse from `CommandBuffer.ShotEvents`; decay in FixedUpdate; velocity mixed in `PlayerMovementSystem`.

---

## 9. Configuration & authoring

| Mechanism | Role |
|-----------|------|
| `WeaponDefinition` | ScriptableObject: Visuals (в т.ч. `MuzzleOffset`, зеркала, базовый поворот), Stats, recoil, shoot FX, `WeaponKind`, блоки shotgun/laser/melee |
| `PlayerAuthoring` | Movement base speed, isometric axis toggles |
| `EcsCombatBootstrap` serialized fields | Player root transform, optional starting weapon |
| Scene/prefab wiring | **Operational risk:** bootstrap must exist and be configured in loaded scenes |

---

## 10. Integrations & external boundaries

**Confirmed Unity API usage in ECS systems:**

- `Input` (axes, mouse buttons, mouse position)
- `Camera.main` + `ScreenToWorldPoint` (input/aim)
- `Physics2D` overlap / queries (melee hits)
- `Object.Instantiate` / `Destroy` (spawn paths via command buffer playback)
- `Time.deltaTime` / `Time.fixedDeltaTime` (passed into world; not `Time.time` in audited cooldown path)

**Not found in audited ECS combat path:**

- Networking / multiplayer replication
- Server authoritative simulation
- Addressables load pipeline in ECS scripts (may exist elsewhere — not verified)

---

## 11. Documentation discrepancies (implementation wins)

| Document | Issue | Статус / источник истины |
|----------|--------|--------------------------|
| `docs/architecture/overview.md` | Упоминание устаревшей версии Unity | Проверять `ProjectSettings/ProjectVersion.txt` |
| `docs/flows/runtime-loop.md` | Порядок систем | **Синхронизирован** с `EcsCombatBootstrap.RegisterSystems` (см. §7.1 этого файла) |
| `docs/flows/player-combat-flow.md` | Раньше: fire rate и `Time.time` | В тексте зафиксировано: cooldown **относительный**, `deltaTime` |
| `docs/models/ecs-components.md` | Неполный каталог боевых компонентов | **Обновлено** (оружие, типы, FX) |
| `docs/gides/add_weapon.md` | Краткий список без Visuals/melee | **Обновлено** (таблица, melee, пивот в Sprite Editor) |
| `README.md` (repo root) | Таблица систем может отставать | Bootstrap + `Assets/Scripts/ECS/Systems/*.cs` |

**Рекомендация:** при смене порядка систем обновлять `docs/flows/runtime-loop.md` и §7.1 в этом аудите.

---

## 12. Engineering assessment

### 12.1 Cohesion / coupling

- **High cohesion** within `Chronocaust.Ecs.Systems` for combat vertical slice.
- **Coupling** to Unity input/rendering/physics is **intentional** for rapid 2D iteration; cost is **testability** (systems are not pure functions of abstract ports without harness).

### 12.2 Extensibility

- New weapon archetype: add tag + data component + system + bootstrap registration + `WeaponPickupSystem` tag migration — **pattern is repeatable** but **manual** (risk of forgetting one switch).

### 12.3 Performance-sensitive paths

- Queries cached with `??=` pattern (good).
- `Physics2D.OverlapCircleNonAlloc` in melee (bounded buffer).
- Lists for spawn payloads in shoot systems — **pre-allocated** in several systems; still **per-frame clear** (acceptable for small counts; profile if many entities).

### 12.4 Concurrency

Single-threaded Unity game loop; **no** custom job system in audited ECS folder.

---

## 13. Testing, CI/CD, observability

| Topic | Status |
|-------|--------|
| Automated tests | **Not present** (no `Tests/**/*.cs` found) |
| CI (GitHub Actions, etc.) | **Not present** |
| Structured logging / metrics | **Not audited**; systems use `Debug.Log` in places (e.g. melee hit placeholder) |
| Feature flags / remote config | **Not found** in audited paths |

---

## 14. Security & abuse surface (brief)

- Single-player assumptions; **no** authn/authz model in ECS layer.
- Input is local `Input` only — **no** network validation story.

---

## 15. Technical debt & architectural risks

| Item | Severity | Notes |
|------|----------|-------|
| Stale internal docs | **Moderate** | Onboarding hazard; fix by regenerating flow docs from bootstrap |
| Scene wiring / single bootstrap | **Moderate** | Missing or duplicate bootstrap = silent failure or double world |
| `Camera.main` / missing camera | **Moderate** | `PlayerInputSystem` returns early if null — player can lose input path |
| Melee damage | **Low–moderate** | Hit detection exists; damage pipeline still TODO (`Debug.Log` placeholder) |
| `WeaponViewSystem` complexity | **Low–moderate** | Позиция/поворот/flip + idle melee aim smoothing + зеркала из Definition; пивот арта — в Sprite Editor |
| Third-party under `Assets/_External` | **Low** for ECS, **medium** for repo hygiene | Increases search noise and upgrade surface |

---

## 16. Scaling constraints (realistic)

- **Entity count:** archetype model scales; **Unity object churn** (instantiate projectiles/flashes/beams) will dominate before ECS CPU does for typical 2D counts.
- **Multiplayer:** would require **new** layer (state replication, prediction); not in codebase.

---

## 17. Operational notes

- **Player movement:** `EcsCombatBootstrap.DisableLegacyMovementController` disables `IsometricPlayerMovementController` when ECS owns movement — **both must not fight** the same `Rigidbody2D`.
- **Destroy callback:** `BindDestroyCallback` destroys `TransformComponent.Transform.gameObject` — **assumption:** entity always owns that hierarchy root for destroyed entities.

---

## 18. Glossary (project-specific)

| Term | Meaning |
|------|---------|
| `ComponentSignature` | Bitmask of component type IDs; archetype key |
| `CommandBuffer` | Deferred structural changes + spawn queues + shot events |
| `WeaponKind` | Authoring enum driving which weapon systems participate |
| `MeleeDataComponent` | Melee tuning + **runtime** attack state + idle aim smooth state |
| `ShotEvent` | Lightweight per-frame event list for recoil / future FX |

---

## 19. Suggested follow-ups (prioritized)

1. **Refresh** `docs/flows/runtime-loop.md` and `player-combat-flow.md` from `EcsCombatBootstrap` (this audit §7). *(Частично сделано в текущем цикле документации.)*  
2. **Fix** Unity version line in `docs/architecture/overview.md`.  
3. **Expand** `docs/models/ecs-components.md` to match `CombatComponents.cs` or generate a table from code. *(Обновлено: оружие, типы, FX.)*  
4. Add **minimal playmode test** or **EditMode** test for `MeleeDataSetup` / `MeleeWeaponPose` pure functions (fast win for regression).  
5. Extract **weapon mount resolution** from `WeaponViewSystem` into a testable static helper (reduces risk as rules grow).

---

*End of audit. For day-to-day ECS rules, `.cursorrules` and `README.md` remain useful; treat this file as the **implementation reconciliation** layer.*
