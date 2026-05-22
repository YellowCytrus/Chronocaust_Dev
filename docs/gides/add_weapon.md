# Добавление оружия (модульный prefab + ECS)

Оружие собирается как **конструктор**: на GameObject лежат MonoBehaviour-модули (`IWeaponModuleAuthoring`), при Play `WeaponEntityBaker` создаёт ECS-сущность с нужными компонентами.

См. также: [Компоненты ECS](../models/ecs-components.md), [Поток боя игрока](../flows/player-combat-flow.md).

---

## 1) Модули authoring

Папка: `Assets/Scripts/ECS/Authoring/Weapon/`

| Модуль | ECS-компоненты |
|--------|----------------|
| `WeaponVisualAuthoring` | `WeaponSpriteComponent`, `WeaponAttachComponent` |
| `ProjectileFireAuthoring` | `ProjectileVisualComponent`, `ProjectileBallisticsComponent`, `FireRateComponent` |
| `RecoilAuthoring` | `RecoilOnFireComponent` |
| `MuzzleFlashAuthoring` | `MuzzleFlashConfigComponent` |
| `MovementSpeedModifierAuthoring` | `MovementSpeedModifierComponent` |
| `ShotgunSpreadAuthoring` | `ShotgunSpreadComponent` |
| `LaserBeamAuthoring` | `LaserBeamConfigComponent` |
| `FireRateAuthoring` | `FireRateComponent` (для лазера без снаряда) |
| `MeleeAttackAuthoring` | `MeleeAttackConfigComponent`, `FireRateComponent` |
| `DamageAuthoring` | `DamageComponent` (заготовка) |
| `AmmoMagazineAuthoring` | `AmmoMagazineComponent` (заготовка) |

**Пресеты:**

- **Пистолет:** Visual + ProjectileFire + Recoil + MuzzleFlash  
- **Дробовик:** Visual + ProjectileFire + ShotgunSpread + Recoil + MuzzleFlash  
- **Лазер:** Visual + LaserBeam + FireRate + MuzzleFlash  
- **Melee:** Visual + MeleeAttack (+ MovementSpeedModifier / MuzzleFlash по желанию)

Не вешайте `ProjectileFireAuthoring` и `MeleeAttackAuthoring` на один объект — `GroundWeaponAuthoring` предупредит в OnValidate.

---

## 2) Лежащее оружие на сцене

1. GameObject + `SpriteRenderer` (видимость на земле)  
2. `GroundWeaponAuthoring`  
3. Нужные модули из таблицы выше  

При Play `EcsCombatBootstrap` вызывает `WeaponEntityBaker.BakeGroundWeapon`.

**Legacy:** поле `definition` (`WeaponDefinition` SO) всё ещё работает через `WeaponDefinitionBaker`, пока сцена не мигрирована. В меню: **Chronocaust → ECS → Migrate Selected Ground Weapons**.

---

## 3) Стартовое оружие игрока

На `EcsCombatBootstrap` укажите **Starting Weapon Source** — GameObject (prefab или сцена) с теми же модулями.

---

## 4) Подбор

`WeaponPickupSystem` копирует модульные компоненты через `WeaponEquipTransfer` (снять старые модули → добавить новые). Признак экипировки: `WeaponSpriteComponent` + `FireRateComponent`.

---

## 5) Системы

| Capability | Система |
|------------|---------|
| Projectile / shotgun / laser | `WeaponFireCoordinatorSystem` |
| Melee | `MeleeAttackSystem` |
| Визуал | `WeaponViewSystem` |
| Отдача | `RecoilApplySystem` + `RecoilDecaySystem` |

---

## Чеклист

- [ ] На ground-объекте есть `GroundWeaponAuthoring` и хотя бы Visual + режим (Projectile / Melee / Laser / Shotgun)  
- [ ] На сцене есть `EcsCombatBootstrap` с Player Transform  
- [ ] У игрока `Rigidbody2D`  
- [ ] Play: подбор **E**, стрельба **ЛКМ**
