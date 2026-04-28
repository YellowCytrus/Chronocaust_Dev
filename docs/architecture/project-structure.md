# Структура проекта

## Верхнеуровневая структура (с точки зрения рантайма)
- `Assets/`: сцены, префабы, скрипты, контент.
- `Packages/`: зависимости Unity Package Manager.
- `ProjectSettings/`: настройки движка и ввода.
- `Library/`, `Temp/`, `Logs/`: сгенерированные/editor-артефакты (не являются источником истины).

Связанные документы: [Обзор архитектуры](overview.md), [Конфигурация](../config/configuration.md), [Сборка и релиз](../deployment/build-release.md).

## Структура скриптов
- `Assets/Scripts/`
  - `ECS/`
    - `Core/`: `EcsWorld`, `EcsEntity`, интерфейсы.
    - `Components/`: компоненты игровых данных.
    - `Systems/`: системы `Update`/`FixedUpdate`.
    - `EcsCombatBootstrap.cs`: bootstrap мира и создание сущности игрока.
  - Legacy-скрипты:
    - `IsometricPlayerMovementController.cs`
    - `IsometricCharacterRenderer.cs`
    - `BasicCameraFollow.cs`
    - `HideTilemapColliderOnPlay.cs`

## Структура контента
- `Assets/Scenes/`: игровые сцены и примеры.
- `Assets/Prefabs/`: префабы игрока и окружения.
- `Assets/Tilemaps/`: изометрические/hex тайлмапы.
- `Assets/_External/`: импортированные demo/framework ассеты.

Сопоставление структуры с поведением см. в [Цикл выполнения](../flows/runtime-loop.md) и [Поток боя игрока](../flows/player-combat-flow.md).

## Примечания по владению
- ECS-код является главным источником прикладной логики для движения и боя игрока.
- YAML префабов/сцен все еще ссылается на legacy-скрипт движения; bootstrap отключает его в рантайме при наличии.

Для границ миграции см. [Backend](../backend/overview.md) и [Глоссарий](../glossary/terms.md).
