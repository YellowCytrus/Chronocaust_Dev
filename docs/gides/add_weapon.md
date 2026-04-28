гайд с нуля под текущую архитектуру (с GroundWeaponAuthoring + WeaponDefinition).

1) Создай ассет оружия (шаблон)
В Project: Create -> Chronocaust -> ECS -> Weapon Definition
Назови, например, WD_Pistol
Заполни поля:
WeaponSprite — спрайт оружия в руках
ProjectileSprite — спрайт пули/снаряда
FireRate
ProjectileSpeed
ProjectileLifetime
MuzzleOffset
Это шаблон данных оружия, без позиции в мире.

2) Подготовь сцену и bootstrap
На сцене должен быть объект с EcsCombatBootstrap
В EcsCombatBootstrap должен быть назначен Player Transform
Игрок должен иметь Rigidbody2D (иначе bootstrap ругнется)
3) Создай лежащее оружие на сцене
Создай GameObject, например GroundWeapon_Pistol
Поставь его в нужную позицию на карте
Добавь SpriteRenderer (чтобы видеть предмет на земле)
Добавь компонент GroundWeaponAuthoring
В GroundWeaponAuthoring -> Definition укажи WD_Pistol
Готово: это авторинг-объект дропа.

4) Что произойдет в Play Mode
При старте EcsCombatBootstrap:

найдет все GroundWeaponAuthoring на сцене
создаст для каждого ECS-сущность с:
GroundWeaponTagComponent
TransformComponent
WeaponComponent (заполненный из WeaponDefinition)
5) Подбор и использование
Подойди к предмету
Нажми E
WeaponPickupSystem:
найдет ближайшее оружие в радиусе
скопирует его WeaponComponent игроку
удалит ground-entity через CommandBuffer.DestroyEntity(...)
Объект на земле исчезнет (через destroy callback)
Игрок сразу начнет:
отображать новое оружие
стрелять его параметрами по ЛКМ
6) Быстрый чек-лист, если не работает
На предмете есть GroundWeaponAuthoring
В Definition назначен WeaponDefinition
В сцене есть EcsCombatBootstrap и корректный Player Transform
Подходишь достаточно близко (радиус подбора в WeaponPickupSystem)
Нажимаешь именно E в Play режиме
