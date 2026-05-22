# ECS: Философия и принципы разработки

Этот документ — для всех, кто приходит в проект с опытом ООП.
Здесь не описывается конкретный API. Здесь объясняется **способ думать**.

---

## Почему ECS, а не ООП

В классическом ООП игра выглядит так:

```
class Player : MonoBehaviour {
    private Gun _gun;
    private Health _health;
    private Animator _animator;

    void Update() {
        _gun.Shoot(Input.GetMouseButton(0));
        _animator.SetFloat("speed", GetSpeed());
    }
}
```

На первый взгляд — нормально. Но за этим кодом стоит фундаментальная проблема: **поведение и данные перемешаны**. `Player` — это одновременно и хранилище данных, и исполнитель логики, и знает про `Gun`, и знает про `Health`, и знает про `Animator`.

Когда появится второй персонаж — напишешь `Enemy : MonoBehaviour` с похожей логикой. Когда появятся 200 врагов — каждый живёт в своём объекте на куче, процессор гоняется по памяти в случайном порядке. Когда захочешь добавить щит только некоторым врагам — добавишь `bool hasShield` и ветку `if`. Кодовая база начнёт гнить.

ECS решает это иначе:

> Сущность — это не объект с поведением. Это просто номер.
> Данные хранятся отдельно от логики, в непрерывных массивах.
> Поведение — чистые функции над данными.

---

## Три концепции, которые нужно принять

### 1. Сущность — это ID, не объект

```csharp
EntityId player = world.CreateEntity(signature);
```

`player` — это число. У него нет методов. Он ничего не знает. Он только позволяет найти данные в массивах.

Перестань думать «у игрока есть пушка». Думай:
**«Есть сущность с компонентами `PlayerTag`, `EquippedWeaponComponent`, `TransformComponent`»**.

Компонент `EquippedWeaponComponent` не принадлежит игроку — он просто лежит в чанке архетипа, который соответствует этому набору типов. Игрок — это не владелец данных. Он — адрес в таблице.


### 2. Компонент — это только данные

Есть два типа «оружия» как компонента:
- `WeaponComponent`: **данные оружия, лежащего на земле**. Содержит спрайты, скорости, множители скорости передвижения, **`WeaponKind`**, блоки параметров под shotgun/laser/melee и т.д. Никогда не находится на игроке.
- `EquippedWeaponComponent`: **данные оружия, экипированного сущностью**. Заполняется из `WeaponComponent` при подборе или из **`WeaponDefinition`** при старте, если на `EcsCombatBootstrap` задано стартовое оружие. Пустые поля означают отсутствие оружия.

```csharp
public struct EquippedWeaponComponent : IEcsComponent
{
    public Sprite WeaponSprite;
    public float FireRate;
    // ... другие поля ...

    public bool HasWeapon => WeaponSprite != null && FireRate > 0f;
}
```

Никаких методов. Никакой логики. Никакого `Shoot()`. Компонент — это строка в таблице базы данных. Он описывает состояние, а не поведение.

Частая ошибка при переходе с ООП: писать компоненты как классы с методами.

```csharp
// НЕПРАВИЛЬНО
public class WeaponComponent : IEcsComponent
{
    public float FireRate;
    public void Shoot() { ... }  // логика внутри данных — нарушение
}
```

Метод `Shoot` должен жить в системе, не в компоненте.

### 3. Система — это функция, не объект

```csharp
public sealed class WeaponShootSystem : IEcsUpdateSystem
{
    public void Update(EcsWorld world, float deltaTime)
    {
        _query.ForEach((EntityId id, ref WeaponComponent weapon, ...) =>
        {
            // читаем данные, пишем данные, ничего больше
        });
    }
}
```

Система не знает про конкретного игрока. Она знает про **набор компонентов**. Она запросит у мира всех сущностей, у которых есть `EquippedWeaponComponent`, `AimComponent`, `InputStateComponent` — и обработает каждого одинаково. Если завтра появится враг с оружием — он автоматически попадёт в этот же запрос.

Система — чистая функция. Единственное состояние, которое она может хранить — это кешированный запрос (`EcsQuery`). Никаких ссылок на конкретные сущности, никаких глобальных переменных.

---

## Архетип — ключевое понятие

Архетип — это уникальная комбинация типов компонентов.

| Сущность | Компоненты | Архетип |
|---|---|---|
| Игрок | `PlayerTag`, `Transform`, `Input`, `EquippedWeapon`, `Movement` | Архетип A |
| Враг | `Transform`, `Health`, `Movement`, `AI` | Архетип B |
| Снаряд | `Transform`, `Projectile` | Архетип C |

Все сущности с одинаковым набором компонентов живут в одном архетипе — рядом в памяти, в одних и тех же массивах (`T[]`). Когда система итерирует снаряды, она проходит по одному плотному массиву `ProjectileComponent[]` без пропусков. Никакого pointer chasing. CPU prefetch работает на полную.

Когда ты добавляешь компонент сущности во время игры — она **мигрирует** в другой архетип: данные копируются в новый чанк. Это структурное изменение. Поэтому оно запрещено внутри итерации — только через `CommandBuffer`.

---

## Правила мышления

### Правило 1: Думай данными, не объектами

Не задавай вопрос «что умеет этот объект?».
Задавай вопрос «какие данные описывают это состояние?».

Нужен щит? Добавь `ShieldComponent { float Value; }`.
Нужно заморозить врага? Добавь `FrozenComponent { float Duration; }`.
Враг умер? Добавь `DeadTagComponent` — или просто удали `HealthComponent`.

Поведение появляется из **наличия или отсутствия компонентов**, а не из иерархии классов. Также, данные могут модифицировать другие данные: например, `EquippedWeaponComponent.MovementSpeedMultiplier` изменяет эффективную скорость из `MovementComponent.BaseSpeed`.

### Правило 2: Система не знает про конкретную сущность

Система работает с запросом, не с экземпляром.

```csharp
// НЕПРАВИЛЬНО — система хранит ссылку на конкретного игрока
public class SomeSystem : IEcsUpdateSystem
{
    private EntityId _playerEntity; // нельзя
}
```

Если система должна обрабатывать только игрока — используй `PlayerTagComponent` как фильтр в запросе. Архетип с этим тегом будет только у игрока.

### Правило 3: Разделяй симуляцию и рендеринг

Физика, движение, кулдауны, урон — `FixedUpdate` системы.
Анимации, спрайты, вращение оружия — `Update` системы.

`FixedUpdate` системы никогда не вызывают `Renderer.*`, `Animator.*`, `transform.rotation =`. Они только меняют данные компонентов. `Update` системы читают эти данные и отражают их в Unity-объектах.

Пример: `PlayerMovementSystem` читает `MovementComponent.BaseSpeed` и `EquippedWeaponComponent.MovementSpeedMultiplier`, умножает их и пишет `Movement.LastDirection`. `CharacterAnimationSystem` читает это поле и вызывает `SetDirection()`. Они никогда не смешиваются.

### Правило 4: Структурные изменения — только через CommandBuffer

Если внутри `ForEach` тебе нужно добавить или удалить компонент — не делай это напрямую. Итерация идёт по чанку, и изменение архетипа разрушит массив под ногами итератора.

```csharp
// НЕПРАВИЛЬНО — прямо внутри ForEach
world.AddComponent(id, new ShieldComponent());

// ПРАВИЛЬНО — отложенно
world.CommandBuffer.AddComponent(id, new ShieldComponent { Value = 50f });
```

`CommandBuffer` применяется после того как все системы отработали за кадр.

### Правило 5: Время — это данные

Никогда не пиши `Time.time` внутри системы.

```csharp
// НЕПРАВИЛЬНО
if (Time.time > cooldown.NextShotTime) { ... }

// ПРАВИЛЬНО
cooldown.CooldownRemaining -= deltaTime;
if (cooldown.CooldownRemaining <= 0f) { ... }
```

`Time.time` — глобальное состояние. Система с `Time.time` нетестируема в изоляции и несовместима с детерминированным воспроизведением. Относительные таймеры (`CooldownRemaining -= deltaTime`) работают независимо от глобального времени.

### Правило 6: Не создавай объекты Unity внутри систем

`new GameObject()`, `AddComponent<SpriteRenderer>()` — это не ECS-операции. Они не детерминированы, они медленные, они нарушают разделение слоёв.

Создание Unity-объектов происходит в двух местах:
- **Bootstrap** — при инициализации мира
- **CommandBuffer** — как часть деferred spawn (например, снаряды)

Внутри `ForEach` — никогда.

---

## Как добавить новую механику

Разберём пошагово на примере: **щит, который поглощает урон**.

### Шаг 1: Определи данные

```csharp
public struct ShieldComponent : IEcsComponent
{
    public float Current;
    public float Max;
}
```

### Шаг 2: Добавь компонент нужным сущностям

В Bootstrap или через CommandBuffer во время игры:

```csharp
// Игрок сразу рождается со щитом — добавь в сигнатуру:
ComponentSignature sig = ComponentSignature.Empty
    .With<PlayerTagComponent>()
    .With<ShieldComponent>()   // добавили
    // ...

// Или дать щит конкретной сущности в рантайме:
world.CommandBuffer.AddComponent(entityId, new ShieldComponent { Current = 50f, Max = 50f });
```

### Шаг 3: Напиши систему

```csharp
public sealed class ShieldRegenSystem : IEcsUpdateSystem
{
    private EcsQuery<ShieldComponent> _query;

    public void Update(EcsWorld world, float deltaTime)
    {
        _query ??= world.CreateQuery<ShieldComponent>();

        _query.ForEach((EntityId id, ref ShieldComponent shield) =>
        {
            if (shield.Current < shield.Max)
            {
                shield.Current = Math.Min(shield.Current + 5f * deltaTime, shield.Max);
            }
        });
    }
}
```

### Шаг 4: Зарегистрируй систему

В `EcsCombatBootstrap.RegisterSystems`:

```csharp
world.AddSystem(new ShieldRegenSystem());
```

Это всё. Ни одного `if (player.hasShield)`. Ни одного наследования. Сущности, у которых есть `ShieldComponent`, автоматически обрабатываются системой — враги, игрок, нейтральные объекты — все одинаково.

---

## Типичные ошибки при переходе с ООП

| ООП-привычка | Как думать в ECS |
|---|---|
| `player.TakeDamage(10)` | Система читает `HealthComponent`, записывает новое значение |
| `enemy.SetState(Frozen)` | Добавить `FrozenComponent` через CommandBuffer |
| `if (unit.isDead) Destroy(unit)` | Система проверяет `hp <= 0`, вызывает `world.CommandBuffer.DestroyEntity(id)` |
| `Gun.Shoot()` — метод на объекте | `WeaponShootSystem` читает `EquippedWeaponComponent`, записывает спавн в CommandBuffer |
| `GameManager.Instance.GetPlayer()` | `PlayerTagComponent` — запрос вернёт единственную matching сущность |
| Наследование: `FlyingEnemy : Enemy` | Разные наборы компонентов: добавь `FlyingComponent` — и система полёта подхватит |

---

## Золотые правила в одну строчку

- **Компонент — строка в таблице, не объект.** (Разделяй `WeaponComponent` на земле и `EquippedWeaponComponent` у сущностей)
- **Система — функция над таблицей, не метод объекта.**
- **Сущность — индекс, не контейнер.**
- **Архетип — определяет, что умеет сущность. Не иерархия классов.**
- **Добавил компонент — сущность стала другой. Убрал компонент — тоже.**
- **Структурные изменения — после итерации, через CommandBuffer.**
- **Время — параметр deltaTime, не глобальный Time.time.**
- **Unity-объекты — только в Bootstrap и CommandBuffer, не в системах.**
