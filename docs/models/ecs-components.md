# Компоненты ECS

Документ описывает игровые компоненты и зоны ответственности, с акцентом на бой и оружие.

Связанные документы: [Обзор архитектуры](../architecture/overview.md), [Поток боя игрока](../flows/player-combat-flow.md), [Гайд: оружие](../gides/add_weapon.md), [Обработка ошибок](../errors/error-handling.md).

## Роли сущностей

- **Игрок:** идентификатор, transform, ввод, движение, прицеливание, экипировка, cooldown, отдача, опционально тег/данные типа оружия, ссылки на рендер персонажа и **визуал оружия**.
- **Снаряд:** transform + `ProjectileComponent` и связанные поля.
- **Лежащее оружие:** `GroundWeaponTagComponent` + `WeaponComponent` (данные шаблона с земли); на игроке не вешается.

## Каталог компонентов

### Теги и базовое состояние

- **`PlayerTagComponent`** — сущность обрабатывается как игрок.
- **`TransformComponent`** — ссылка на Unity `Transform` сущности.
- **`InputStateComponent`** — ввод: стрельба/взаимодействие, мышь в мире, движение.
- **`AimComponent`** — нормализованное направление прицела (2D).
- **`MovementComponent`** — базовая скорость, изометрические оси, последнее направление движения (для анимации).
- **`RigidbodyComponent`** — ссылка на `Rigidbody2D`.
- **`CharacterRenderComponent`** — ссылка на `IsometricCharacterRenderer`.

### Оружие

- **`WeaponComponent`** — полный набор полей **шаблона** для предмета на земле (спрайты, скорости, `MuzzleOffset`, визуальные зеркала, `WeaponKind`, блоки shotgun/laser/**melee** и т.д.). На игроке **не** используется напрямую после подбора.
- **`EquippedWeaponComponent`** — срез данных для **экипированного** оружия (спрайты, скорострельность, смещения, отдача, зеркала, `WeaponVisualBaseRotationDeg`, …). `HasWeapon` завязан на спрайт и `FireRate`. Заполняется из `WeaponComponent` при подборе или из `WeaponDefinition` при старте с `startingWeapon`.
- **`WeaponCooldownComponent`** — оставшееся время до следующего «выстрела»/удара; уменьшается на `deltaTime` в системах стрельбы/melee (**без** `Time.time`).
- **`WeaponViewComponent`** — ссылки на **`Transform`** и **`SpriteRenderer`** дочернего объекта **WeaponView** (создаётся в `EcsCombatBootstrap.CreateWeaponViewObject`: один GameObject, спрайт на нём). Обновляет **`WeaponViewSystem`** (позиция = `MuzzleOffset` + melee pose в пространстве прицела, поворот, flip).

### Типы оружия (взаимоисключающие теги + данные)

- **`ShotgunTagComponent`** + **`ShotgunDataComponent`**
- **`LaserTagComponent`** + **`LaserDataComponent`**
- **`MeleeTagComponent`** + **`MeleeDataComponent`** — настройки ближнего боя (движение, дальность, окна урона, фазы, `MeleeViewSuppressFlipY`, сглаживание idle-прицела и **рантайм-состояние атаки**: фаза, время, направление заморозки, список попаданий).

### Снаряды, лучи, FX

- **`ProjectileComponent`** — направление, скорость, оставшееся время жизни.
- **`BeamComponent`**, **`MuzzleFlashComponent`** — визуалы луча и вспышки (ссылки на Unity-объекты).
- **`RecoilComponent`** — накопленная скорость отдачи.

### Земля

- **`GroundWeaponTagComponent`** — маркер сущности подбираемого оружия.

## Правила владения данными

- Компоненты хранят состояние; поведение в системах.
- Ссылки на Unity-объекты — осознанный компромисс для 2D-итерации.
- Создание игрока и `WeaponView` — в bootstrap; снаряды/лучи/вспышки — через очереди `CommandBuffer` после симуляции.

Последовательность систем: [Цикл выполнения](../flows/runtime-loop.md). Настройки оружия в редакторе: [Гайд: оружие](../gides/add_weapon.md).
