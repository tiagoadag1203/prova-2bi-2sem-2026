using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XequeDeTinta.Battle;
using XequeDeTinta.Core;
using XequeDeTinta.Data;

namespace XequeDeTinta.UI
{
    /// <summary>Painéis das casas do tabuleiro: Descanso, Loja, Evento e Baú. Cada um chama onDone ao fechar.</summary>
    public static class TilePanels
    {
        static RectTransform Modal(Transform canvas, string name, Vector2 size, out GameObject root)
        {
            var overlay = Panels.Overlay(canvas, name);
            root = overlay.gameObject;
            return UIFactory.CreatePanel("Painel", overlay, new Vector2(0.5f, 0.5f), size, Vector2.zero);
        }

        static TextMeshProUGUI TopText(RectTransform panel, string name, string text, float size, Color color, float y, float h)
        {
            var t = UIFactory.CreateText(name, panel, text, size, color);
            t.rectTransform.anchorMin = new Vector2(0, 1); t.rectTransform.anchorMax = new Vector2(1, 1);
            t.rectTransform.pivot = new Vector2(0.5f, 1);
            t.rectTransform.offsetMin = new Vector2(10, 0); t.rectTransform.offsetMax = new Vector2(-10, 0);
            t.rectTransform.sizeDelta = new Vector2(t.rectTransform.sizeDelta.x, h);
            t.rectTransform.anchoredPosition = new Vector2(0, -y);
            return t;
        }

        // ---------- Descanso ----------

        public static GameObject Rest(Transform canvas, RunState run, Action onDone)
        {
            var panel = Modal(canvas, "PainelDescanso", new Vector2(360, 170), out var root);
            TopText(panel, "Titulo", "Descanso", 12, Palette.Gold, 8, 16);
            var info = TopText(panel, "Info", "", 8, Palette.Cream, 32, 30);

            Button heal = null, upgrade = null, leave = null;
            void Refresh() => info.text = $"Vida {run.Hp}/{run.MaxHp}\nEscolha uma coisa para fazer";
            Refresh();

            void Finish(string message)
            {
                info.text = message;
                if (heal != null) Destroy(heal); if (upgrade != null) Destroy(upgrade);
                leave.gameObject.SetActive(true);
            }

            int amount = RestLogic.HealAmount(run);
            heal = UIFactory.CreateButton("Curar", panel, $"Curar {amount} de vida", new Vector2(0.5f, 0), new Vector2(150, 20), new Vector2(0, 60), () =>
            {
                int healed = RestLogic.Rest(run);
                Finish($"Você descansa e recupera {healed} de vida.\nVida {run.Hp}/{run.MaxHp}");
            });

            bool anyUpgradable = RestLogic.UpgradableCards(run).Any();
            upgrade = UIFactory.CreateButton("Melhorar", panel, "Melhorar 1 carta", new Vector2(0.5f, 0), new Vector2(150, 20), new Vector2(0, 34), () =>
            {
                GameObject picker = null;
                var list = RestLogic.UpgradableCards(run).ToList();
                picker = Panels.CardPicker(canvas, "Escolha a carta para melhorar", list, card =>
                {
                    RestLogic.Upgrade(run, card);
                    Destroy(picker);
                    Finish($"{card.Data.displayName} foi melhorada!");
                }, () => Destroy(picker), "Voltar");
            });
            upgrade.interactable = anyUpgradable;

            leave = UIFactory.CreateButton("Continuar", panel, "Continuar", new Vector2(0.5f, 0), new Vector2(110, 20), new Vector2(0, 10), () => { Destroy(root); onDone(); });
            leave.gameObject.SetActive(false);
            return root;
        }

        // ---------- Baú ----------

        public static GameObject Chest(Transform canvas, RunState run, Action onDone)
        {
            var panel = Modal(canvas, "PainelBau", new Vector2(360, 170), out var root);
            TopText(panel, "Titulo", "Baú!", 12, Palette.Gold, 8, 16);
            var res = ChestLogic.Open(run);
            string text = $"Você encontra {res.Coins} moedas";
            text += res.Relic != null ? $"\ne a relíquia {res.Relic.displayName}:\n{res.Relic.description}" : "\n(e já tem todas as relíquias).";
            TopText(panel, "Texto", text, 8, Palette.Cream, 34, 60);
            UIFactory.CreateButton("Continuar", panel, "Continuar", new Vector2(0.5f, 0), new Vector2(110, 20), new Vector2(0, 10), () => { Destroy(root); onDone(); });
            return root;
        }

        // ---------- Evento ----------

        public static GameObject Event(Transform canvas, RunState run, EventData ev, Action onDone)
        {
            var panel = Modal(canvas, "PainelEvento", new Vector2(440, 210), out var root);
            TopText(panel, "Titulo", ev.title, 12, Palette.Gold, 8, 16);
            var body = TopText(panel, "Texto", ev.body, 8, Palette.Cream, 30, 70);

            Button a = null, b = null, leave = null;

            void Show(EventResult res)
            {
                body.text = string.Join("\n", res.Lines);
                if (a != null) Destroy(a.gameObject); if (b != null) Destroy(b.gameObject);
                leave.gameObject.SetActive(true);
            }

            void Choose(EventChoice choice)
            {
                var res = EventLogic.Apply(run, choice);
                if (res.RemoveCardPending)
                {
                    GameObject picker = null;
                    picker = Panels.CardPicker(canvas, "Escolha a carta para remover", new List<CardInstance>(run.Deck), card =>
                    {
                        run.RemoveCard(card);
                        res.Lines.Add($"Você remove {card.Data.displayName} do deck.");
                        Destroy(picker);
                        Show(res);
                    }, () => { Destroy(picker); }, "Voltar");
                    return;
                }
                Show(res);
            }

            a = UIFactory.CreateButton("EscolhaA", panel, Trim(ev.choiceA.text), new Vector2(0.5f, 0), new Vector2(400, 22), new Vector2(0, 40), () => Choose(ev.choiceA), 8);
            b = UIFactory.CreateButton("EscolhaB", panel, Trim(ev.choiceB.text), new Vector2(0.5f, 0), new Vector2(400, 22), new Vector2(0, 12), () => Choose(ev.choiceB), 8);
            leave = UIFactory.CreateButton("Continuar", panel, "Continuar", new Vector2(0.5f, 0), new Vector2(110, 20), new Vector2(0, 10), () => { Destroy(root); onDone(); });
            leave.gameObject.SetActive(false);
            return root;
        }

        static string Trim(string s) => s;

        // ---------- Loja ----------

        public static GameObject Shop(Transform canvas, RunState run, Core.Shop shop, Action onDone)
        {
            var panel = Modal(canvas, "PainelLoja", new Vector2(580, 320), out var root);
            TopText(panel, "Titulo", "Loja", 12, Palette.Gold, 8, 16);
            var coins = UIFactory.CreateText("Moedas", panel, "", 8, Palette.Gold, TextAlignmentOptions.TopRight, false);
            coins.rectTransform.anchorMin = coins.rectTransform.anchorMax = new Vector2(1, 1); coins.rectTransform.pivot = new Vector2(1, 1);
            coins.rectTransform.sizeDelta = new Vector2(150, 10); coins.rectTransform.anchoredPosition = new Vector2(-10, -10);
            var msg = TopText(panel, "Msg", "Clique para comprar", 8, Palette.Cream, 26, 10);

            void Refresh() => coins.text = $"Moedas {run.Coins}";
            Refresh();

            var widgets = new Dictionary<ShopItem, GameObject>();
            void Buy(ShopItem item, CardInstance remove = null)
            {
                var r = shop.Buy(run, item, remove);
                switch (r)
                {
                    case BuyResult.Ok: msg.text = $"Comprou {item.Label}!"; if (widgets.TryGetValue(item, out var w)) w.SetActive(false); break;
                    case BuyResult.NotEnoughCoins: msg.text = "Moedas insuficientes!"; break;
                    case BuyResult.HpFull: msg.text = "Sua vida já está cheia."; break;
                    case BuyResult.DeckTooSmall: msg.text = "Deck pequeno demais."; break;
                    case BuyResult.NeedsCardToRemove: msg.text = "Escolha uma carta."; break;
                }
                Refresh();
            }

            // Cartas
            var cards = shop.Items.Where(i => i.Kind == ShopItemKind.Card).ToList();
            for (int i = 0; i < cards.Count; i++)
            {
                var item = cards[i];
                float x = (i - (cards.Count - 1) / 2f) * 100f - 150f;
                var holder = UIFactory.Anchored("Item" + i, panel, new Vector2(0.5f, 0.5f), new Vector2(92, 160), new Vector2(x, 0));
                widgets[item] = holder.gameObject;
                var cv = CardView.Create(holder, new CardInstance(item.Card), new Vector2(0, 26));
                cv.Rect.anchorMin = cv.Rect.anchorMax = new Vector2(0.5f, 0); 
                UIFactory.CreateButton("Comprar", holder, $"Comprar {item.Price}", new Vector2(0.5f, 0), new Vector2(88, 18), new Vector2(0, 2), () => Buy(item));
            }

            // Relíquia, cura e remover (coluna da direita)
            float col = 180f;
            var relic = shop.Items.FirstOrDefault(i => i.Kind == ShopItemKind.Relic);
            if (relic != null)
            {
                var box = UIFactory.Anchored("Relica", panel, new Vector2(0.5f, 0.5f), new Vector2(190, 58), new Vector2(col, 40));
                widgets[relic] = box.gameObject;
                var bg = box.gameObject.AddComponent<Image>(); bg.color = Palette.PurpleLight;
                var t = UIFactory.CreateText("Texto", box, $"{relic.Relic.displayName}\n{relic.Relic.description}", 8, Palette.Cream, TextAlignmentOptions.TopLeft);
                t.rectTransform.offsetMin = new Vector2(4, 22); t.rectTransform.offsetMax = new Vector2(-4, -3);
                t.enableAutoSizing = true; t.fontSizeMin = 5; t.fontSizeMax = 8;
                UIFactory.CreateButton("Comprar", box, $"Comprar {relic.Price}", new Vector2(0.5f, 0), new Vector2(120, 16), new Vector2(0, 3), () => Buy(relic));
            }
            var heal = shop.Items.First(i => i.Kind == ShopItemKind.Heal);
            var healBtn = UIFactory.CreateButton("Curar", panel, $"Curar {run.Config.shopHealAmount} ({heal.Price})", new Vector2(0.5f, 0.5f), new Vector2(190, 20), new Vector2(col, -10), () => Buy(heal));
            widgets[heal] = healBtn.gameObject;
            var rem = shop.Items.First(i => i.Kind == ShopItemKind.RemoveCard);
            var remBtn = UIFactory.CreateButton("Remover", panel, $"Remover carta ({rem.Price})", new Vector2(0.5f, 0.5f), new Vector2(190, 20), new Vector2(col, -36), () =>
            {
                if (rem.Sold) return;
                if (run.Coins < rem.Price) { msg.text = "Moedas insuficientes!"; return; }
                GameObject picker = null;
                picker = Panels.CardPicker(canvas, "Escolha a carta para remover", new List<CardInstance>(run.Deck), card =>
                {
                    Destroy(picker);
                    Buy(rem, card);
                }, () => Destroy(picker), "Voltar");
            });
            widgets[rem] = remBtn.gameObject;

            UIFactory.CreateButton("Sair", panel, "Sair da loja", new Vector2(0.5f, 0), new Vector2(120, 20), new Vector2(0, 8), () => { Destroy(root); onDone(); });
            return root;
        }

        static void Destroy(GameObject go) { if (go != null) UnityEngine.Object.Destroy(go); }
        static void Destroy(Button b) { if (b != null) UnityEngine.Object.Destroy(b.gameObject); }
    }
}
