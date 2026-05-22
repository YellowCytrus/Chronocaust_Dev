using Chronocaust.Ecs.Components;
using UnityEngine;
using UnityEngine.UI;

namespace Chronocaust.Ecs
{
    /// <summary>
    /// Builds the combat HUD Canvas in the editor scene. Bootstrap reads the view via
    /// <see cref="TryGetView"/> and stores it on the player entity as <see cref="PlayerHudViewComponent"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerHudAuthoring : MonoBehaviour
    {
        [SerializeField] private bool buildOnAwake = true;

        private PlayerHudViewComponent _view;
        private GameObject _canvasRoot;
        private bool _built;
        private int _layoutVersion;

        public bool TryGetView(out PlayerHudViewComponent view)
        {
            if (!_built && buildOnAwake)
            {
                EnsureBuilt();
            }

            view = _view;
            return _built;
        }

        public void EnsureBuilt()
        {
            if (_built && _layoutVersion == CombatHudTheme.LayoutVersion)
            {
                return;
            }

            if (_canvasRoot != null)
            {
                Destroy(_canvasRoot);
                _canvasRoot = null;
            }

            _view = BuildHud();
            _canvasRoot = _view.HealthPanelRoot != null
                ? _view.HealthPanelRoot.transform.parent.gameObject
                : null;
            _layoutVersion = CombatHudTheme.LayoutVersion;
            _built = true;
        }

        private static PlayerHudViewComponent BuildHud()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Sprite uiSprite = Resources.GetBuiltinResource<Sprite>("UISprite.psd");

            GameObject canvasGo = new GameObject("CombatHudCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            Transform root = canvasGo.transform;

            GameObject healthPanel = CreatePanel(root, "HealthPanel",
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 20f), new Vector2(300f, 96f));

            Text healthLabel = CreateTextBand(healthPanel.transform, "HealthLabel", font, 18,
                TextAnchor.MiddleLeft, CombatHudTheme.AccentFrost,
                new Vector2(0f, 1f), new Vector2(0.35f, 1f),
                new Vector2(14f, -10f), new Vector2(0f, -38f));
            healthLabel.text = "HP";
            AddTextShadow(healthLabel, CombatHudTheme.PanelBackground);

            Text healthValue = CreateTextBand(healthPanel.transform, "HealthValue", font, 18,
                TextAnchor.MiddleRight, CombatHudTheme.TextPrimary,
                new Vector2(0.35f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -10f), new Vector2(-14f, -38f));
            healthValue.text = "100 / 100";
            AddTextShadow(healthValue, CombatHudTheme.PanelBackground);

            Image healthFill = CreateFillBar(healthPanel.transform, "HealthFill", uiSprite,
                CombatHudTheme.AccentFrost,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(14f, 12f), new Vector2(-14f, 44f));

            GameObject weaponPanel = CreatePanel(root, "WeaponPanel",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(380f, 168f));

            GameObject iconSlot = CreateRect(weaponPanel.transform, "IconSlot",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(96f, 96f));

            Image iconBg = CreateImage(iconSlot.transform, "IconBg", uiSprite, CombatHudTheme.IconSlotBg,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            AddOutline(iconBg, CombatHudTheme.PanelBorder, 1f);

            Image weaponIcon = CreateImage(iconSlot.transform, "WeaponIcon", uiSprite, Color.white,
                new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);
            weaponIcon.preserveAspect = true;

            Image cooldownRing = CreateImage(iconSlot.transform, "CooldownRing", uiSprite,
                new Color(CombatHudTheme.AccentFrost.r, CombatHudTheme.AccentFrost.g,
                    CombatHudTheme.AccentFrost.b, 0.35f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            cooldownRing.type = Image.Type.Simple;
            AddOutline(cooldownRing, CombatHudTheme.AccentFrostDim, 1.2f);

            Image cooldownOverlay = CreateImage(iconSlot.transform, "CooldownOverlay", uiSprite,
                CombatHudTheme.CooldownTint,
                new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero);
            cooldownOverlay.type = Image.Type.Filled;
            cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
            cooldownOverlay.fillOrigin = (int)Image.Origin360.Top;
            cooldownOverlay.fillClockwise = true;
            cooldownOverlay.fillAmount = 0f;

            GameObject textColumn = CreateStretchRect(weaponPanel.transform, "TextColumn",
                new Vector2(118f, 10f), new Vector2(-12f, -10f));

            Text weaponName = CreateTextBand(textColumn.transform, "WeaponName", font, 21,
                TextAnchor.UpperLeft, CombatHudTheme.TextPrimary,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -4f), new Vector2(0f, -32f));
            AddTextShadow(weaponName, CombatHudTheme.PanelBackground);

            Text weaponStats = CreateTextBand(textColumn.transform, "WeaponStats", font, 15,
                TextAnchor.UpperLeft, CombatHudTheme.TextMuted,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -36f), new Vector2(0f, -58f));

            Image cooldownBarBg = CreateImage(textColumn.transform, "CooldownBarBg", uiSprite,
                CombatHudTheme.PanelInset,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -74f), new Vector2(0f, -62f));
            Image cooldownBarFill = CreateImage(cooldownBarBg.transform, "CooldownBarFill", uiSprite,
                CombatHudTheme.AccentFrostDim,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            cooldownBarFill.type = Image.Type.Filled;
            cooldownBarFill.fillMethod = Image.FillMethod.Horizontal;
            cooldownBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            cooldownBarFill.fillAmount = 0f;

            Text hintText = CreateTextBand(textColumn.transform, "HintText", font, 14,
                TextAnchor.LowerLeft, CombatHudTheme.AccentWarm,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 38f), new Vector2(0f, 58f));
            AddTextShadow(hintText, CombatHudTheme.PanelBackground);

            Image slot0Frame = CreateImage(weaponPanel.transform, "Slot0Frame", uiSprite,
                CombatHudTheme.PanelInset,
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(14f, 12f), new Vector2(58f, 58f));
            Image slot0Icon = CreateImage(slot0Frame.transform, "Slot0Icon", uiSprite, Color.white,
                new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f), Vector2.zero, Vector2.zero);
            slot0Icon.preserveAspect = true;
            AddOutline(slot0Frame, CombatHudTheme.PanelBorder, 1f);

            Image slot1Frame = CreateImage(weaponPanel.transform, "Slot1Frame", uiSprite,
                CombatHudTheme.PanelInset,
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(64f, 12f), new Vector2(108f, 58f));
            Image slot1Icon = CreateImage(slot1Frame.transform, "Slot1Icon", uiSprite, Color.white,
                new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f), Vector2.zero, Vector2.zero);
            slot1Icon.preserveAspect = true;
            AddOutline(slot1Frame, CombatHudTheme.PanelBorder, 1f);

            weaponName.text = "Unarmed";
            weaponStats.text = "—";
            hintText.text = "[1][2] slots  ·  [G] drop  ·  [E] pick up";
            weaponIcon.enabled = false;

            return new PlayerHudViewComponent
            {
                HealthLabelText = healthLabel,
                HealthValueText = healthValue,
                HealthFill = healthFill,
                WeaponIcon = weaponIcon,
                CooldownOverlay = cooldownOverlay,
                CooldownBarFill = cooldownBarFill,
                Slot0Icon = slot0Icon,
                Slot1Icon = slot1Icon,
                Slot0Frame = slot0Frame,
                Slot1Frame = slot1Frame,
                WeaponNameText = weaponName,
                WeaponStatsText = weaponStats,
                HintText = hintText,
                HealthPanelRoot = healthPanel,
                WeaponPanelRoot = weaponPanel
            };
        }

        private static GameObject CreatePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
        {
            GameObject panel = CreateRect(parent, name, anchorMin, anchorMax, anchoredPos, size);
            Image bg = panel.AddComponent<Image>();
            bg.sprite = Resources.GetBuiltinResource<Sprite>("UISprite.psd");
            bg.type = Image.Type.Sliced;
            bg.color = CombatHudTheme.PanelBackground;
            bg.raycastTarget = false;
            AddOutline(bg, CombatHudTheme.PanelBorder, 1.5f);
            return panel;
        }

        private static GameObject CreateRect(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            bool onePoint = anchorMin == anchorMax;
            rt.pivot = onePoint ? anchorMin : new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return go;
        }

        private static GameObject CreateStretchRect(Transform parent, string name,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return go;
        }

        private static Image CreateFillBar(Transform parent, string name, Sprite sprite, Color fillColor,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject barBg = new GameObject(name + "_Bg", typeof(RectTransform));
            barBg.transform.SetParent(parent, false);
            RectTransform bgRt = barBg.GetComponent<RectTransform>();
            bgRt.anchorMin = anchorMin;
            bgRt.anchorMax = anchorMax;
            bgRt.offsetMin = offsetMin;
            bgRt.offsetMax = offsetMax;

            Image bgImg = barBg.AddComponent<Image>();
            bgImg.sprite = sprite;
            bgImg.color = CombatHudTheme.PanelInset;
            bgImg.raycastTarget = false;

            GameObject fillGo = new GameObject(name, typeof(RectTransform));
            fillGo.transform.SetParent(barBg.transform, false);
            RectTransform fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(2f, 2f);
            fillRt.offsetMax = new Vector2(-2f, -2f);

            Image fill = fillGo.AddComponent<Image>();
            fill.sprite = sprite;
            fill.color = fillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            fill.raycastTarget = false;
            return fill;
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static Text CreateTextBand(Transform parent, string name, Font font, int fontSize,
            TextAnchor alignment, Color color, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            Text text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = color;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static void AddTextShadow(Text text, Color shadowColor)
        {
            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(shadowColor.r, shadowColor.g, shadowColor.b, 0.85f);
            shadow.effectDistance = new Vector2(1f, -1f);
        }

        private static void AddOutline(Graphic graphic, Color color, float distance)
        {
            Outline outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
        }
    }
}
