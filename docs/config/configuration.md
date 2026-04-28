# Конфигурация

Проект настраивается через настройки Unity, inspector-поля сцен/префабов и зависимости из package manifest.

Связанные документы: [Структура проекта](../architecture/project-structure.md), [Сборка и релиз](../deployment/build-release.md), [Цикл выполнения](../flows/runtime-loop.md).

## Движок и пакеты
- Версия Unity: `2020.3.49f1`.
- В `Packages/manifest.json` подключены пакеты 2D tilemap/pixel-perfect и стандартные Unity-модули.

## Поверхности конфигурации геймплея
- Сериализованные поля `EcsCombatBootstrap`:
  - ссылка на transform игрока
  - скорость движения и векторы изометрических осей
  - спрайты оружия/снаряда
  - скорострельность, скорость снаряда, время жизни снаряда
  - смещение дула
- Ввод опирается на имена осей legacy Input Manager:
  - `Horizontal`, `Vertical`
  - кнопка мыши 0 для стрельбы

## Требования к wiring сцен и префабов
- В активной сцене должен быть объект с `EcsCombatBootstrap`.
- На объекте игрока должен быть `Rigidbody2D`.
- В визуальной иерархии игрока должен быть `IsometricCharacterRenderer`.

Поведение валидации и отказов описано в [Обработка ошибок](../errors/error-handling.md). Карта компонентов — в [Компоненты ECS](../models/ecs-components.md).
