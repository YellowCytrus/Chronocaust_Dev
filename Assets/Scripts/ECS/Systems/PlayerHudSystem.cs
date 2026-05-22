using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Chronocaust.Ecs.Systems
{
    /// <summary>
    /// Rendering layer: mirrors ECS combat state onto screen-space UGUI.
    /// </summary>
    public sealed class PlayerHudSystem : IEcsUpdateSystem
    {
        private EcsQuery<PlayerTagComponent, HealthComponent, WeaponLoadoutComponent,
            EquippedWeaponComponent, WeaponCooldownComponent, PlayerHudViewComponent> _query;

        public void Update(EcsWorld world, float deltaTime)
        {
            _query ??= world.CreateQuery<PlayerTagComponent, HealthComponent, WeaponLoadoutComponent,
                EquippedWeaponComponent, WeaponCooldownComponent, PlayerHudViewComponent>();

            _query.ForEach((EntityId id,
                ref PlayerTagComponent _,
                ref HealthComponent health,
                ref WeaponLoadoutComponent loadout,
                ref EquippedWeaponComponent equipped,
                ref WeaponCooldownComponent cooldown,
                ref PlayerHudViewComponent hud) =>
            {
                if (hud.HealthFill == null)
                {
                    return;
                }

                float ratio = health.Ratio;
                hud.HealthFill.fillAmount = ratio;
                hud.HealthFill.color = ratio <= 0.25f
                    ? CombatHudTheme.HealthLow
                    : CombatHudTheme.AccentFrost;

                int current = Mathf.CeilToInt(Mathf.Max(0f, health.Current));
                int max = Mathf.CeilToInt(Mathf.Max(1f, health.Max));

                if (hud.HealthLabelText != null)
                {
                    hud.HealthLabelText.text = "HP";
                    hud.HealthLabelText.gameObject.SetActive(true);
                }

                if (hud.HealthValueText != null)
                {
                    hud.HealthValueText.text = $"{current} / {max}";
                    hud.HealthValueText.gameObject.SetActive(true);
                }

                bool hasWeapon = equipped.HasWeapon;
                ComponentSignature sig = world.GetSignature(id);
                float cooldown01 = ComputeCooldown01(hasWeapon, in equipped, in cooldown);

                if (hud.WeaponIcon != null)
                {
                    hud.WeaponIcon.sprite = hasWeapon ? equipped.WeaponSprite : null;
                    hud.WeaponIcon.enabled = hasWeapon && equipped.WeaponSprite != null;
                    hud.WeaponIcon.color = hasWeapon
                        ? Color.white
                        : new Color(0.55f, 0.65f, 0.72f, 0.45f);
                }

                UpdateSlotIcon(hud.Slot0Icon, hud.Slot0Frame, in loadout.Slot0, loadout.ActiveIndex == 0);
                UpdateSlotIcon(hud.Slot1Icon, hud.Slot1Frame, in loadout.Slot1, loadout.ActiveIndex == 1);

                if (hud.WeaponNameText != null)
                {
                    string slotTag = loadout.ActiveIndex == 0 ? "[1]" : "[2]";
                    hud.WeaponNameText.text = hasWeapon
                        ? $"{slotTag} {ResolveWeaponName(in equipped, hasWeapon, sig)}"
                        : $"{slotTag} Unarmed";
                }

                if (hud.WeaponStatsText != null)
                {
                    hud.WeaponStatsText.text = hasWeapon
                        ? BuildWeaponStats(world, id, in equipped, sig)
                        : "<color=#9BB4C4>Empty slot — pick up or switch</color>";
                }

                if (hud.CooldownOverlay != null)
                {
                    hud.CooldownOverlay.fillAmount = cooldown01;
                    hud.CooldownOverlay.enabled = cooldown01 > 0.001f;
                }

                if (hud.CooldownBarFill != null)
                {
                    hud.CooldownBarFill.fillAmount = cooldown01;
                    bool showBar = hasWeapon;
                    hud.CooldownBarFill.enabled = showBar;
                    if (hud.CooldownBarFill.transform.parent != null)
                    {
                        hud.CooldownBarFill.transform.parent.gameObject.SetActive(showBar);
                    }
                }

                if (hud.HintText != null)
                {
                    hud.HintText.text = "[1][2] switch  ·  [G] drop  ·  [E] pick up";
                }
            });
        }

        private static void UpdateSlotIcon(Image icon, Image frame, in WeaponSlotEntry entry, bool isActive)
        {
            if (icon == null)
            {
                return;
            }

            bool has = entry.HasWeapon;
            icon.sprite = has ? entry.WeaponSprite : null;
            icon.enabled = has && entry.WeaponSprite != null;
            icon.color = has ? Color.white : new Color(1f, 1f, 1f, 0.2f);

            if (frame != null)
            {
                frame.color = isActive
                    ? new Color(CombatHudTheme.AccentFrost.r, CombatHudTheme.AccentFrost.g,
                        CombatHudTheme.AccentFrost.b, 0.45f)
                    : CombatHudTheme.PanelInset;
                AddOutlineColor(frame, isActive ? CombatHudTheme.AccentFrost : CombatHudTheme.PanelBorder);
            }
        }

        private static void AddOutlineColor(Graphic graphic, Color color)
        {
            Outline outline = graphic.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = color;
            }
        }

        private static float ComputeCooldown01(bool hasWeapon, in EquippedWeaponComponent equipped,
            in WeaponCooldownComponent cooldown)
        {
            if (!hasWeapon || equipped.FireRate <= 0f)
            {
                return 0f;
            }

            float interval = 1f / equipped.FireRate;
            return interval > 0f
                ? Mathf.Clamp01(cooldown.CooldownRemaining / interval)
                : 0f;
        }

        private static string ResolveWeaponName(in EquippedWeaponComponent equipped, bool hasWeapon,
            ComponentSignature sig)
        {
            if (!hasWeapon)
            {
                return "Unarmed";
            }

            if (!string.IsNullOrWhiteSpace(equipped.DisplayName))
            {
                return equipped.DisplayName;
            }

            if (sig.Has<MeleeTagComponent>()) return "Melee";
            if (sig.Has<LaserTagComponent>()) return "Laser";
            if (sig.Has<ShotgunTagComponent>()) return "Shotgun";
            return "Sidearm";
        }

        private static string BuildWeaponStats(EcsWorld world, EntityId id,
            in EquippedWeaponComponent equipped, ComponentSignature sig)
        {
            if (equipped.FireRate <= 0f)
            {
                return string.Empty;
            }

            float interval = 1f / equipped.FireRate;
            string line = $"<color=#5EC4C8>{equipped.FireRate:0.#}</color> shots/s"
                + $"  ·  reload {interval:0.##}s";

            if (Mathf.Abs(equipped.MovementSpeedMultiplier - 1f) > 0.01f)
            {
                line += $"  ·  move <color=#E6B873>x{equipped.MovementSpeedMultiplier:0.##}</color>";
            }

            if (sig.Has<ShotgunTagComponent>())
            {
                ref ShotgunDataComponent shotgun = ref world.GetComponent<ShotgunDataComponent>(id);
                int pellets = shotgun.PelletCount > 0 ? shotgun.PelletCount : 8;
                line += $"  ·  {pellets} pellets / {shotgun.SpreadAngle:0}°";
            }
            else if (sig.Has<LaserTagComponent>())
            {
                ref LaserDataComponent laser = ref world.GetComponent<LaserDataComponent>(id);
                line += $"  ·  beam {laser.BeamDuration:0.##}s";
            }
            else if (sig.Has<MeleeTagComponent>())
            {
                ref MeleeDataComponent melee = ref world.GetComponent<MeleeDataComponent>(id);
                line += $"  ·  range {melee.Range:0.#}  /  {melee.ArcAngle:0}°";
            }
            else if (equipped.ProjectileSpeed > 0f)
            {
                line += $"  ·  spd {equipped.ProjectileSpeed:0.#}";
            }

            return line;
        }
    }
}
