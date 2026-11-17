using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace XequeDeTinta.UI
{
    /// <summary>Paleta da interface (ASSETS.md): roxo escuro, dourado e creme.</summary>
    public static class Palette
    {
        public static readonly Color Purple = new Color32(0x2A, 0x1B, 0x3D, 0xFF);
        public static readonly Color PurpleLight = new Color32(0x4A, 0x35, 0x66, 0xFF);
        public static readonly Color Gold = new Color32(0xF2, 0xC1, 0x4E, 0xFF);
        public static readonly Color Cream = new Color32(0xF4, 0xE9, 0xD8, 0xFF);
        public static readonly Color Red = new Color32(0xE0, 0x4F, 0x4F, 0xFF);
        public static readonly Color Green = new Color32(0x6F, 0xCF, 0x6F, 0xFF);
        public static readonly Color Shadow = new Color(0f, 0f, 0f, 0.6f);
    }

    /// <summary>
    /// Constrói elementos de uGUI por código, no tamanho de referência 640x360.
    /// As telas ficam finas: cenas e painéis só chamam isto.
    /// </summary>
    public static class UIFactory
    {
        public const float RefWidth = 640f, RefHeight = 360f;

        static TMP_FontAsset font;
        public static TMP_FontAsset Font
        {
            get
            {
                if (font == null) font = Resources.Load<TMP_FontAsset>("Fonts/PressStart2P SDF");
                return font;
            }
        }

        /// <summary>Sprites opcionais de painel/botão (9-slice). Se nulos, usa retângulos coloridos.</summary>
        public static Sprite PanelSprite, ButtonSprite;

        public static Canvas CreateCanvas(string name, int sortingOrder = 0, Transform parent = null)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null) go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        /// <summary>Garante um EventSystem com o módulo do Input System.</summary>
        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = anchorMin; r.anchorMax = anchorMax; r.offsetMin = offsetMin; r.offsetMax = offsetMax;
            return r;
        }

        /// <summary>Retângulo de tamanho fixo ancorado/pivotado no mesmo ponto (0..1) do pai.</summary>
        public static RectTransform Anchored(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = r.pivot = anchor;
            r.sizeDelta = size;
            r.anchoredPosition = position;
            return r;
        }

        public static Image CreateImage(string name, Transform parent, Color color, Sprite sprite = null)
        {
            var r = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var img = r.gameObject.AddComponent<Image>();
            img.color = color; img.sprite = sprite;
            if (sprite != null) img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            return img;
        }

        public static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center, bool wrap = true)
        {
            var r = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            if (Font != null) t.font = Font;
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Painel creme com borda dourada. Devolve o retângulo interno para o conteúdo.</summary>
        public static RectTransform CreatePanel(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 position)
        {
            var outer = Anchored(name, parent, anchor, size, position);
            var border = outer.gameObject.AddComponent<Image>();
            border.color = Palette.Gold; border.sprite = PanelSprite; border.raycastTarget = true;
            if (PanelSprite != null) border.type = Image.Type.Sliced;
            var inner = Rect("Fundo", outer, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));
            var bg = inner.gameObject.AddComponent<Image>();
            bg.color = Palette.Purple; bg.raycastTarget = false;
            return inner;
        }

        public static Button CreateButton(string name, Transform parent, string label, Vector2 anchor, Vector2 size, Vector2 position,
            Action onClick, float fontSize = 8f)
        {
            var r = Anchored(name, parent, anchor, size, position);
            var img = r.gameObject.AddComponent<Image>();
            img.color = Palette.Cream; img.sprite = ButtonSprite;
            if (ButtonSprite != null) img.type = Image.Type.Sliced;
            var btn = r.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.9f, 0.6f);
            colors.pressedColor = new Color(0.8f, 0.7f, 0.4f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            var text = CreateText("Texto", r, label, fontSize, Palette.Purple);
            text.rectTransform.offsetMin = new Vector2(3, 2);
            text.rectTransform.offsetMax = new Vector2(-3, -2);
            return btn;
        }

        public static void SetButtonLabel(Button b, string label)
        {
            var t = b.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) t.text = label;
        }
    }
}
