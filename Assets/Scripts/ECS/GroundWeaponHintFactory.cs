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

            root.AddComponent<GraphicRaycaster>();

            RectTransform panelRt = CreateChild(root.transform, "Panel");
            panelRt.sizeDelta = new Vector2(220f, 56f);
            Image panel = panelRt.gameObject.AddComponent<Image>();
            panel.sprite = Resources.GetBuiltinResource<Sprite>("UISprite.psd");
            panel.color = CombatHudTheme.PanelBackground;
            panel.raycastTarget = false;
            Outline outline = panelRt.gameObject.AddComponent<Outline>();
            outline.effectColor = CombatHudTheme.PanelBorder;
            outline.effectDistance = new Vector2(1f, -1f);

            RectTransform textRt = CreateChild(panelRt, "Label");
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(8f, 6f);
            textRt.offsetMax = new Vector2(-8f, -6f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Text label = textRt.gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = 16;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = CombatHudTheme.AccentWarm;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.text = "[E] Pick up";

            Shadow shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            shadow.effectDistance = new Vector2(1f, -1f);

            root.SetActive(false);

            return new GroundWeaponHintViewComponent
            {
                Root = root.transform,
                Label = label
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
