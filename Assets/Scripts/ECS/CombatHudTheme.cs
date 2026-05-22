using UnityEngine;

namespace Chronocaust.Ecs
{
    /// <summary>
    /// HUD palette aligned with the project's pseudo-isometric frozen biomes:
    /// slate ice panels, frost cyan accents, snow-bright text (see docs/architecture).
    /// </summary>
    internal static class CombatHudTheme
    {
        public const int LayoutVersion = 3;

        public static readonly Color PanelBackground = new Color(0.06f, 0.10f, 0.16f, 0.94f);
        public static readonly Color PanelInset = new Color(0.04f, 0.07f, 0.11f, 0.95f);
        public static readonly Color PanelBorder = new Color(0.42f, 0.62f, 0.78f, 1f);
        public static readonly Color AccentFrost = new Color(0.37f, 0.80f, 0.78f, 1f);
        public static readonly Color AccentFrostDim = new Color(0.28f, 0.55f, 0.58f, 1f);
        public static readonly Color AccentWarm = new Color(0.90f, 0.72f, 0.45f, 1f);
        public static readonly Color HealthLow = new Color(0.91f, 0.32f, 0.28f, 1f);
        public static readonly Color TextPrimary = new Color(0.93f, 0.97f, 1f, 1f);
        public static readonly Color TextMuted = new Color(0.62f, 0.76f, 0.86f, 1f);
        public static readonly Color CooldownTint = new Color(0.05f, 0.09f, 0.14f, 0.82f);
        public static readonly Color IconSlotBg = new Color(0.10f, 0.14f, 0.20f, 1f);
    }
}
