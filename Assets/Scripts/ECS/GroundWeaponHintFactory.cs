using Chronocaust.Ecs.Components;
using UnityEngine;
using UnityEngine.UI;

namespace Chronocaust.Ecs
{
    internal static class GroundWeaponHintFactory
    {
        private const float WorldScale = 0.012f;
        private const float HeightOffset = 0.65f;

        public static GroundWeaponHintViewComponent Create(Transform weaponTransform)
        {
            GameObject root = new GameObject("PickupHint");
            root.transform.SetParent(weaponTransform, false);
            root.transform.localPosition = new Vector3(0f, HeightOffset, 0f);
            root.transform.localScale = Vector3.one * WorldScale;

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            CanvasGroup group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            RectTransform textRt = CreateChild(root.transform, "Label");
            textRt.sizeDelta = new Vector2(300f, 44f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Text label = textRt.gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = 18;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = CombatHudTheme.AccentWarm;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.text = "[E] Pick up";

            Shadow shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);

            root.SetActive(false);

            return new GroundWeaponHintViewComponent
            {
                Root = root.transform,
                Label = label,
                Group = group
            };
        }

        private static RectTransform CreateChild(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }
    }
}
