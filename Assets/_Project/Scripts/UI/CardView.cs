using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XequeDeTinta.Battle;
using XequeDeTinta.Data;

namespace XequeDeTinta.UI
{
    /// <summary>Uma carta na tela: moldura tingida pelo elemento, nome, custo, descrição. Hover aumenta; clique joga.</summary>
    public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public const float Width = 88f, Height = 116f;

        public CardInstance Card { get; private set; }
        public RectTransform Rect { get; private set; }
        public bool Hovered { get; private set; }
        public bool Playable { get; private set; } = true;

        public event Action<CardView> Clicked;
        public event Action<CardView, bool> HoverChanged;

        Image frame, body;
        TextMeshProUGUI nameText, costText, descText, elementText;

        /// <summary>Sprite opcional da moldura (lote 2). Se nulo, usa retângulos coloridos.</summary>
        public static Sprite FrameSprite;

        public static Color ElementColor(Element e)
        {
            switch (e)
            {
                case Element.Fire: return new Color32(0xD9, 0x50, 0x3A, 0xFF);
                case Element.Plant: return new Color32(0x4F, 0xA5, 0x5B, 0xFF);
                case Element.Water: return new Color32(0x4A, 0x86, 0xD6, 0xFF);
                default: return new Color32(0xB8, 0xAE, 0x9C, 0xFF);
            }
        }

        public static string ElementName(Element e)
        {
            switch (e)
            {
                case Element.Fire: return "Fogo";
                case Element.Plant: return "Planta";
                case Element.Water: return "Água";
                default: return "Neutro";
            }
        }

        public static CardView Create(Transform parent, CardInstance card, Vector2 position)
        {
            var go = new GameObject("Carta_" + card.Data.id, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<CardView>();
            view.Build(card);
            view.Rect.anchoredPosition = position;
            return view;
        }

        void Build(CardInstance card)
        {
            Card = card;
            Rect = (RectTransform)transform;
            Rect.anchorMin = Rect.anchorMax = new Vector2(0.5f, 0f);
            Rect.pivot = new Vector2(0.5f, 0f);
            Rect.sizeDelta = new Vector2(Width, Height);

            frame = GetComponent<Image>();
            frame.sprite = FrameSprite;
            frame.color = ElementColor(card.Element);

            var inner = UIFactory.Rect("Corpo", transform, Vector2.zero, Vector2.one, new Vector2(3, 3), new Vector2(-3, -3));
            body = inner.gameObject.AddComponent<Image>();
            body.color = Palette.Cream; body.raycastTarget = false;

            var band = UIFactory.Rect("Faixa", inner, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -24), new Vector2(0, 0));
            var bandImg = band.gameObject.AddComponent<Image>();
            bandImg.color = ElementColor(card.Element); bandImg.raycastTarget = false;

            nameText = UIFactory.CreateText("Nome", band, card.Name, 8, Color.white);
            nameText.rectTransform.offsetMin = new Vector2(19, 1); nameText.rectTransform.offsetMax = new Vector2(-2, -1);
            nameText.enableAutoSizing = true; nameText.fontSizeMin = 5; nameText.fontSizeMax = 8;

            // Custo em um selo dourado no canto.
            var badge = UIFactory.Anchored("Custo", inner, new Vector2(0, 1), new Vector2(15, 15), new Vector2(2, -4));
            var badgeImg = badge.gameObject.AddComponent<Image>();
            badgeImg.color = Palette.Gold; badgeImg.raycastTarget = false;
            costText = UIFactory.CreateText("N", badge, card.Cost.ToString(), 8, Palette.Purple);

            // Ilustração da carta (32x32) logo abaixo da faixa do nome.
            float descTop = -28f;
            if (card.Data.art != null)
            {
                var artRt = UIFactory.Anchored("Arte", inner, new Vector2(0.5f, 1), new Vector2(32, 32), new Vector2(0, -27));
                artRt.pivot = new Vector2(0.5f, 1);
                var artImg = artRt.gameObject.AddComponent<Image>();
                artImg.sprite = card.Data.art; artImg.raycastTarget = false; artImg.preserveAspect = true;
                descTop = -62f;
            }

            descText = UIFactory.CreateText("Descricao", inner, card.Description, 8, Palette.Purple, TextAlignmentOptions.Top);
            descText.rectTransform.offsetMin = new Vector2(4, 16); descText.rectTransform.offsetMax = new Vector2(-4, descTop);
            descText.enableAutoSizing = true; descText.fontSizeMin = 5; descText.fontSizeMax = 8;

            string foot = ElementName(card.Element) + (card.Exhaust ? " · Esgota" : "");
            elementText = UIFactory.CreateText("Rodape", inner, foot, 8, ElementColor(card.Element), TextAlignmentOptions.Bottom);
            elementText.rectTransform.offsetMin = new Vector2(2, 2); elementText.rectTransform.offsetMax = new Vector2(-2, 14);
            elementText.enableAutoSizing = true; elementText.fontSizeMin = 5; elementText.fontSizeMax = 8;

            var art = UIArt.Get();
            var icon = art != null ? art.ElementIcon(card.Element) : null;
            if (icon != null)
            {
                var ir = UIFactory.Anchored("ElementoIcone", inner, new Vector2(0, 0), new Vector2(12, 12), new Vector2(3, 3));
                ir.pivot = new Vector2(0, 0);
                var im = ir.gameObject.AddComponent<Image>();
                im.sprite = icon; im.raycastTarget = false; im.preserveAspect = true;
            }
        }

        /// <summary>Sem energia: carta escurecida (ainda clicável para dar o aviso).</summary>
        public void SetPlayable(bool playable)
        {
            Playable = playable;
            var c = playable ? Color.white : new Color(0.55f, 0.55f, 0.6f, 1f);
            frame.color = ElementColor(Card.Element) * c;
            body.color = Palette.Cream * c;
        }

        public void OnPointerEnter(PointerEventData e) { Hovered = true; HoverChanged?.Invoke(this, true); }
        public void OnPointerExit(PointerEventData e) { Hovered = false; HoverChanged?.Invoke(this, false); }
        public void OnPointerClick(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) Clicked?.Invoke(this); }
    }
}
