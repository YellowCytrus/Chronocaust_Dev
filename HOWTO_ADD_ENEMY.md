# Как добавить врага на сцену

Краткая инструкция под текущую ECS-архитектуру (`EnemyAuthoring` + `EcsCombatBootstrap`).

## Что делает враг в Play Mode

При запуске сцены `EcsCombatBootstrap` находит все объекты с `EnemyAuthoring` и создаёт для каждого ECS-сущность. Дальше без дополнительного кода:

1. **EnemyChaseSystem** — враг идёт к игроку и останавливается на `Stop Distance`.
2. **EnemyAttackSystem** — в радиусе `Attack Range` наносит урон игроку; урон и кулдаун берутся из экипированного оружия (`WeaponDefinition.Damage`, `FireRate`).
3. **WeaponViewSystem** — отображает оружие в руках (если задано `Starting Weapon`).
4. **EnemyDeathSystem** — удаляет врага при `Health <= 0`.
5. Урон от игрока (огнестрел, ближний бой) снижает `Health` врага, если настроены коллайдер и слой (см. ниже).

---

## 1. Подготовь сцену

На сцене уже должно быть:

- объект с **EcsCombatBootstrap**;
- назначенный **Player Transform** (игрок с `PlayerAuthoring` и `Rigidbody2D`).

Без bootstrap враги не появятся в ECS-мире.

---

## 2. Создай ассет оружия (если враг с оружием)

`Create → Chronocaust → ECS → Weapon Definition`

Минимум для боя:

- **Weapon Sprite** — спрайт в руках;
- **Damage** — урон за удар;
- **Fire Rate** — частота атак (кулдаун = `1 / FireRate`).

Остальные поля можно оставить по умолчанию, если враг бьёт в ближнем радиусе без снарядов.

---

## 3. Создай GameObject врага

1. Создай объект, например `Enemy_Slime`.
2. Поставь его на карту рядом с игроком.
3. Добавь **Rigidbody2D** (обязательно — без него преследование не работает).
   - Рекомендуется: `Body Type = Dynamic`, `Gravity Scale = 0` (как у игрока).
4. Добавь **SpriteRenderer** со спрайтом врага (например из `Assets/Textures/Enemies/...`).
5. Добавь **Collider2D** (например `CapsuleCollider2D` или `BoxCollider2D` по размеру спрайта) — без него попадания снарядов и ближнего боя не регистрируются.
6. Поставь объект на слой из **Damageable Layers** у `EcsCombatBootstrap` (например слой `Enemy`, если он есть в проекте).
7. Добавь компонент **Enemy Authoring**.

Опционально: дочерний объект с **IsometricCharacterRenderer** и аниматором — тогда враг будет проигрывать направления бега, как игрок.

---

## 4. Настрой Enemy Authoring

| Поле | Назначение |
|------|------------|
| **Max Health** | Здоровье врага |
| **Attack Range** | Дистанция, с которой враг бьёт игрока |
| **Unarmed Damage** | Урон без оружия (если `Starting Weapon` пуст) |
| **Starting Weapon** | `WeaponDefinition` — урон, кулдаун и спрайт в руках |
| **Move Speed** | Скорость преследования |
| **Stop Distance** | На каком расстоянии враг перестаёт подходить и начинает бить |

Типичный старт: `Max Health = 40`, `Attack Range = 1.2`, `Move Speed = 3.5`, `Stop Distance = 0.85`.

---

## 5. Запуск и проверка

1. Нажми **Play**.
2. Враг должен идти к игроку.
3. В радиусе атаки HP игрока (`PlayerAuthoring → Max Health`) уменьшается.
4. При `Health = 0` у врага объект удаляется с сцены.

Повтори шаги 3–4 для каждого врага — bootstrap подхватывает **все** `EnemyAuthoring` на сцене автоматически.

---

## Быстрый чек-лист, если не работает

| Проблема | Что проверить |
|----------|----------------|
| Враг стоит на месте | Есть `Rigidbody2D` на объекте врага |
| Нет урона по игроку | `Attack Range` достаточный; у оружия `Damage > 0` |
| Слишком редкие удары | Увеличь `Fire Rate` в `WeaponDefinition` |
| Нет спрайта оружия | Заполнен `Starting Weapon` и `Weapon Sprite` в ассете |
| Враг не появляется в ECS | На сцене есть `EcsCombatBootstrap`, в консоли нет ошибок при старте |
| Игрок не получает урон | На игроке есть `PlayerAuthoring`; bootstrap создаёт `HealthComponent` |
| Игрок не наносит урон врагу | Есть `Collider2D` на враге (или дочернем объекте); слой врага входит в **Damageable Layers** на bootstrap |
| Снаряд проходит сквозь врага | Коллайдер не trigger-only без тела, или слой не в маске; проверь **Damageable Layers** |
| Лазер не наносит урон | Тот же `Collider2D` и слой; урон по первому `Raycast` — если луч упирается в стену раньше врага, до врага урон не дойдёт |

---

## Связанные файлы

- `Assets/Scripts/ECS/EnemyAuthoring.cs` — параметры в инспекторе
- `Assets/Scripts/ECS/EcsCombatBootstrap.cs` — спавн врагов и регистрация систем
- `Assets/Scripts/ECS/Systems/EnemyChaseSystem.cs`
- `Assets/Scripts/ECS/Systems/EnemyAttackSystem.cs`
- `Assets/Scripts/ECS/Systems/EnemyDeathSystem.cs`
- `Assets/Scripts/ECS/PhysicsEntityRegistry.cs` — коллайдер → ECS-сущность
- `Assets/Scripts/ECS/CombatDamage.cs`, `CombatTargetRules.cs`
- `Assets/Scripts/ECS/Systems/ProjectileHitSystem.cs`
- `Assets/Scripts/ECS/Systems/LaserBeamSystem.cs` — урон по лучу при попадании в зарегистрированный коллайдер

Подробнее про ECS в целом: [README.md](README.md), [docs/HOWTO_NEW_ENTITY.md](docs/HOWTO_NEW_ENTITY.md).
