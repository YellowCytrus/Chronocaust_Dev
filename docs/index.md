# Документация Chronocaust_Dev

Эта документация описывает текущее состояние Unity 2D проекта с псевдо-изометрией и кастомным ECS-слоем геймплея.

## Начать отсюда
- Для **аудита по реализации** (порядок систем, границы, риски, расхождения с доками): **[ARCHITECTURE_AUDIT.md](ARCHITECTURE_AUDIT.md)**.
- Прочитайте **[ECS: Философия и принципы](ecs-philosophy.md)** — обязательно для всех, кто приходит с опытом ООП.
- Прочитайте [Обзор архитектуры](architecture/overview.md), чтобы понять форму системы и причины выбранных решений.
- Затем откройте [Цикл выполнения](flows/runtime-loop.md), чтобы увидеть порядок исполнения.
- Для структуры игровых данных используйте [Компоненты ECS](models/ecs-components.md).

## Карта документации
- Архитектура: [Обзор](architecture/overview.md), [Структура проекта](architecture/project-structure.md)
- Потоки: [Цикл выполнения](flows/runtime-loop.md), [Поток боя игрока](flows/player-combat-flow.md)
- Модели: [Компоненты ECS](models/ecs-components.md)
- Гайды: [Добавление оружия](gides/add_weapon.md)
- Надежность: [Обработка ошибок](errors/error-handling.md), [Стратегия тестирования](testing/testing-strategy.md)
- Эксплуатация: [Конфигурация](config/configuration.md), [Сборка и релиз](deployment/build-release.md)
- Границы системы: [Backend](backend/overview.md), [База данных](database/overview.md), [API](api/overview.md)
- Общие термины: [Глоссарий](glossary/terms.md)

## Текущее состояние
- Рантайм-архитектура гибридная: жизненный цикл Unity `MonoBehaviour` хостит кастомный ECS-цикл.
- Ключевой игровой поток реализован в ECS: ввод, движение, прицеливание, визуал оружия, стрельба, симуляция снарядов, подтипы оружия (shotgun, laser, melee), отдача.
- Параметры оружия и melee задаются в **`WeaponDefinition`** (в т.ч. Visuals: `MuzzleOffset`, зеркала, базовый поворот спрайта); см. [Гайд: оружие](gides/add_weapon.md).
- Legacy-компонент движения все еще есть на префабе и отключается ECS-bootstrap'ом во время запуска.

Перед расширением систем см. [Известные риски](architecture/overview.md#известные-риски-и-технический-долг) и [Конфигурация](config/configuration.md).
