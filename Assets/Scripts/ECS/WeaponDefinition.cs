using Chronocaust.Ecs.Components;
using UnityEngine;

namespace Chronocaust.Ecs
{
    [CreateAssetMenu(
        fileName = "WeaponDefinition",
        menuName = "Chronocaust/ECS/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [Header("Visuals")]
        [Tooltip("Shown on the combat HUD. Falls back to weapon type name when empty.")]
        public string DisplayName;
        public Sprite WeaponSprite;
        public Sprite ProjectileSprite;
        [Tooltip("Uniform scale applied to the weapon sprite in the WeaponView child object.")]
        [Min(0.01f)] public float WeaponSpriteScale = 1f;
        [Tooltip("Uniform scale applied to the projectile sprite.")]
        [Min(0.01f)] public float ProjectileSpriteScale = 1f;
        public int WeaponSortingOrder = 100;
        public int ProjectileSortingOrder = 90;
        [Tooltip("Постоянный поворот спрайта оружия по Z (градусы) относительно направления прицела: компенсация наклота в текстуре (например −45). Суммируется с углом к курсору и melee-позой.")]
        public float WeaponVisualBaseRotationDeg;
        [Tooltip("Отразить спрайт оружия по горизонтали (SpriteRenderer.flipX).")]
        public bool WeaponVisualMirrorX;
        [Tooltip("Отразить спрайт оружия по вертикали (SpriteRenderer.flipY). Для оружия дальнего боя суммируется с отражением при прицеле влево.")]
        public bool WeaponVisualMirrorY;
        [Tooltip("Смещение в пространстве прицела (+X к курсору, +Y влево по дуге): для дальнего боя — точка дула относительно персонажа; для melee — точка рукояти / крепления на персонаже (сюда суммируется анимационный сдвиг атаки).")]
        public Vector2 MuzzleOffset = new Vector2(0.45f, 0f);

        [Header("Stats")]
        [Tooltip("Damage dealt per successful hit.")]
        [Min(0.1f)] public float Damage = 10f;
        [Min(0.1f)] public float FireRate = 6f;
        [Min(0.1f)] public float ProjectileSpeed = 10f;
        [Min(0.1f)] public float ProjectileLifetime = 2f;

        [Header("Character Modifiers")]
        [Tooltip("Multiplies the bearer's base movement speed. 1 = no change, 0.8 = 20% slower.")]
        [Min(0.01f)] public float MovementSpeedMultiplier = 1f;

        [Header("Recoil")]
        [Tooltip("Kick strength (units/s) applied opposite to the shoot direction on each shot. 0 = no recoil.")]
        [Min(0f)] public float RecoilStrength = 1.5f;
        [Tooltip("Exponential decay rate. Higher = faster recovery. 8 ≈ dissipates in ~0.3 s.")]
        [Min(0.1f)] public float RecoilDecayRate = 8f;

        [Header("Shoot Effect")]
        [Tooltip("Sprite sheet for the muzzle flash (288x48, 6 frames). Drag one of the 4 Shoot_effects PNGs here.")]
        public Sprite[] ShootEffectFrames;
        [Tooltip("Duration of each muzzle flash frame in seconds.")]
        [Min(0.01f)] public float ShootEffectFrameDuration = 0.05f;
        [Tooltip("Uniform scale of the muzzle flash sprite.")]
        [Min(0.01f)] public float ShootEffectScale = 1f;
        [Tooltip("Local offset of the flash relative to the muzzle, in weapon-space (X = forward, Y = up).")]
        public Vector2 ShootEffectMuzzleOffset = Vector2.zero;

        [Header("Weapon Type")]
        [Tooltip("Determines which shoot system handles this weapon.")]
        public WeaponKind Kind = WeaponKind.Default;

        [Header("Shotgun")]
        [Tooltip("Number of pellets fired per shot.")]
        [Min(1)] public int PelletCount = 8;
        [Tooltip("Total spread cone angle in degrees.")]
        [Min(0f)] public float SpreadAngle = 30f;

        [Header("Laser")]
        [Tooltip("How long the beam stays visible (seconds).")]
        [Min(0.01f)] public float BeamDuration = 0.3f;
        [Tooltip("Visual width of the beam line.")]
        [Min(0.01f)] public float BeamWidth = 0.1f;
        public Color BeamColor = Color.red;

        [Header("Melee — общее")]
        [Tooltip("Если включено: не использовать SpriteRenderer.flipY — лево/право только через поворот Z (непрерывный atan2, без скачка при dir.x≈0). Если спрайт ведёт себя странно при углах >90°, выключите на этом ассете.")]
        public bool MeleeViewSuppressFlipY = true;
        [Tooltip("Сглаживание визуального угла прицела в idle melee (экспонента, 1/с). Оружие как «держимый объект», не жёстко к курсору. 0 = мгновенно. На время атаки внутреннее сглаженное значение не обновляется (курсор во время замаха не «уезжает» idle-угол в сторону).")]
        [Min(0f)] public float MeleeIdleVisualAimSmoothHz = 12f;
        [Tooltip("Вид ближней атаки: дуга, тычок, удар по земле или вращение.")]
        public MeleeMotionType MeleeMotionType = MeleeMotionType.Arc;
        [Tooltip("Дальность / радиус проверки попадания (метры). Общий для большинства типов.")]
        [Min(0.1f)] public float MeleeRange = 1.5f;
        [Tooltip("Короткий отвод назад перед ударом (метры), anticipation. Используется всеми типами.")]
        [Min(0f)] public float MeleeAnticipationPull;

        [Header("Melee — дуга")]
        [Tooltip("Ширина сектора удара по дуге (градусы). И для визуала дуги, и для конуса попадания.")]
        [Min(1f)] public float MeleeArcAngle = 90f;

        [Header("Melee — тычок")]
        [Tooltip("Насколько выдвигается остриё вперёд к курсору (метры). Если 0 — берётся MeleeRange.")]
        [Min(0f)] public float MeleeThrustDistance;
        [Tooltip("Радиус проверки попадания у острия (метры).")]
        [Min(0f)] public float MeleeThrustHitRadius;
        [Tooltip("Дополнительный наклон спрайта оружия при тычке (градусы по оси Z), только визуал, на бой не влияет. 0 — выключено. Пик на середине выпада; если копьё «смотрит» не туда — попробуйте отрицательное значение.")]
        public float MeleeThrustVisualTiltMaxDeg;

        [Header("Melee — удар по земле (side-based)")]
        [Tooltip("Фиксированный слэм: курсор только выбирает left/right pose. Без flip спрайта.")]
        public bool MeleeSlamUseFixedAimDir;
        [Tooltip("Мировое смещение оружия от персонажа, пресет «удар справа».")]
        public Vector2 MeleeSlamPoseOffsetRight = new Vector2(0.45f, 0f);
        [Tooltip("Поворот Z, пресет «удар справа» (градусы).")]
        public float MeleeSlamPoseRotRight = -90f;
        [Tooltip("Мировое смещение, пресет «удар слева». Задай вручную — не зеркалится автоматически.")]
        public Vector2 MeleeSlamPoseOffsetLeft;
        [Tooltip("Поворот Z, пресет «удар слева» (градусы).")]
        public float MeleeSlamPoseRotLeft;
        [Tooltip("Смещение вспышки выстрела от ног, правая сторона. Если (0,0) — ShootEffectMuzzleOffset.")]
        public Vector2 MeleeSlamShootEffectOffsetRight;
        [Tooltip("Смещение вспышки, левая сторона. Если (0,0) — ShootEffectMuzzleOffset с отрицательным X.")]
        public Vector2 MeleeSlamShootEffectOffsetLeft;
        [Tooltip("Смещение центра хитбокса по мировой X от ног (метры).")]
        [Min(0f)] public float MeleeSlamHitSideOffset = 0.12f;
        [Tooltip("Дополнительный поворот спрайта по Z на ранней фазе: «завод» перед ударом (градусы). Только визуал позы; знак меняет сторону наклона. 0 в ассете — подставится дефолт в MeleeDataSetup.")]
        public float MeleeSlamWindupDeg;
        [Tooltip("Дополнительный поворот по Z в фазе удара вниз (градусы). Суммируется с анимацией замаха; отрицательное значение инвертирует направление относительно пресета. 0 в ассете — подставится дефолт.")]
        public float MeleeSlamDownDeg;
        [Tooltip("Замах: смещение крепления по мировой Y (вверх), метры. Пресет «справа»; слева — то же с flipY.")]
        public float MeleeSlamWindupOffsetY;
        [Tooltip("Насколько центр хитбокса опущен по мировой Y вниз от позиции персонажа (метры).")]
        [Min(0f)] public float MeleeSlamStrikeDepth;

        [Header("Melee — вращение")]
        [Tooltip("Сколько полных оборотов за одну атаку (обычно 1 = 360°).")]
        [Min(0.1f)] public float MeleeSpinTurns = 1f;

        [Header("Melee — длительность фаз (сек)")]
        [Tooltip("Фаза подготовки: замах, без урона. 0 — значение по умолчанию, если все три фазы нули.")]
        [Min(0f)] public float MeleeStartupDuration;
        [Tooltip("Активная фаза: удар, попадания. 0 — по умолчанию.")]
        [Min(0f)] public float MeleeActiveDuration;
        [Tooltip("Возврат и торможение после удара. 0 — по умолчанию.")]
        [Min(0f)] public float MeleeRecoveryDuration;

        [Header("Melee — окно урона (нормализованное t ∈ [0,1])")]
        [Tooltip("Начало окна урона: t = прошедшее время атаки / полная длительность (замах + удар + возврат), от 0 до 1. Пока t меньше этого значения, урон не считается. 0 и 0 с концом — подставятся пресеты по типу атаки.")]
        [Range(0f, 1f)] public float MeleeHitWindowStartT;
        [Tooltip("Конец окна урона по тому же t. Попадания считаются только в фазе Active и при HitWindowStartT ≤ t ≤ HitWindowEndT.")]
        [Range(0f, 1f)] public float MeleeHitWindowEndT;
    }
}
