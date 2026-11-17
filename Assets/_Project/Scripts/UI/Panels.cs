using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XequeDeTinta.Battle;
using XequeDeTinta.Core;
using XequeDeTinta.Data;

namespace XequeDeTinta.UI
{
    /// <summary>Painéis modais sobre o tabuleiro (recompensa, deck; loja, descanso, evento e baú no M5).</summary>
    public static class Panels
    {
        /// <summary>Fundo escurecido que bloqueia cliques no que está atrás.</summary>
        public static RectTransform Overlay(Transform canvas, string name)
        {
            var r = UIFactory.Rect(name, canvas, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var img = r.gameObject.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.6f);
            img.raycastTarget = true;
            return r;
        }

        /// <summary>Escolha 1 entre as cartas da recompensa (ou pule). onChoice recebe o índice, ou -1 para pular.</summary>
        public static GameObject Reward(Transform canvas, PendingReward reward, Action<int> onChoice)
        {
            var overlay = Overlay(canvas, "PainelRecompensa");
            var panel = UIFactory.CreatePanel("Painel", overlay, new Vector2(0.5f, 0.5f), new Vector2(380, 230), Vector2.zero);

            var title = UIFactory.CreateText("Titulo", panel, $"Vitória! +{reward.Coins} moedas", 12, Palette.Gold);
            title.rectTransform.anchorMin = new Vector2(0, 1); title.rectTransform.anchorMax = new Vector2(1, 1);
            title.rectTransform.pivot = new Vector2(0.5f, 1);
            title.rectTransform.sizeDelta = new Vector2(0, 16); title.rectTransform.anchoredPosition = new Vector2(0, -8);

            var sub = UIFactory.CreateText("Sub", panel, "Escolha 1 carta para o deck, ou pule", 8, Palette.Cream);
            sub.rectTransform.anchorMin = new Vector2(0, 1); sub.rectTransform.anchorMax = new Vector2(1, 1);
            sub.rectTransform.pivot = new Vector2(0.5f, 1);
            sub.rectTransform.sizeDelta = new Vector2(0, 12); sub.rectTransform.anchoredPosition = new Vector2(0, -28);

            int n = reward.Choices.Count;
            for (int i = 0; i < n; i++)
            {
                int idx = i;
                var cv = CardView.Create(panel, new CardInstance(reward.Choices[i]), new Vector2((i - (n - 1) / 2f) * 100f, 36));
                cv.Clicked += _ => onChoice(idx);
            }

            UIFactory.CreateButton("Pular", panel, "Pular", new Vector2(0.5f, 0), new Vector2(80, 18), new Vector2(0, 10), () => onChoice(-1));
            return overlay.gameObject;
        }

        /// <summary>Lista de todas as cartas do deck, com rolagem.</summary>
        public static GameObject Deck(Transform canvas, IReadOnlyList<CardInstance> deck, Action onClose)
        {
            var overlay = Overlay(canvas, "PainelDeck");
            var panel = UIFactory.CreatePanel("Painel", overlay, new Vector2(0.5f, 0.5f), new Vector2(560, 300), Vector2.zero);

            var title = UIFactory.CreateText("Titulo", panel, $"Seu deck ({deck.Count} cartas)", 12, Palette.Gold);
            title.rectTransform.anchorMin = new Vector2(0, 1); title.rectTransform.anchorMax = new Vector2(1, 1);
            title.rectTransform.pivot = new Vector2(0.5f, 1);
            title.rectTransform.sizeDelta = new Vector2(0, 16); title.rectTransform.anchoredPosition = new Vector2(0, -8);

            // Área rolável.
            var viewport = UIFactory.Rect("Viewport", panel, Vector2.zero, Vector2.one, new Vector2(8, 34), new Vector2(-8, -30));
            viewport.gameObject.AddComponent<RectMask2D>();
            var vimg = viewport.gameObject.AddComponent<Image>();
            vimg.color = new Color(0, 0, 0, 0.01f);

            const float scale = 0.8f;
            float cw = CardView.Width * scale + 6f, ch = CardView.Height * scale + 6f;
            int cols = Mathf.Max(1, Mathf.FloorToInt((560f - 16f) / cw));
            int rows = Mathf.CeilToInt(deck.Count / (float)cols);

            var content = UIFactory.Rect("Conteudo", viewport, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = new Vector2(0, Mathf.Max(rows * ch, 1f));
            content.anchoredPosition = Vector2.zero;

            for (int i = 0; i < deck.Count; i++)
            {
                var cv = CardView.Create(content, deck[i], Vector2.zero);
                var rt = cv.Rect;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
                rt.localScale = new Vector3(scale, scale, 1f);
                float x = (i % cols) * cw + 4f, y = -(i / cols) * ch - 2f;
                rt.anchoredPosition = new Vector2(x, y);
            }

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;

            UIFactory.CreateButton("Fechar", panel, "Fechar", new Vector2(0.5f, 0), new Vector2(80, 18), new Vector2(0, 8), () => onClose());
            return overlay.gameObject;
        }
    }
}
