using System;
using System.Collections.Generic;
using XequeDeTinta.Battle;
using XequeDeTinta.Data;

namespace XequeDeTinta.Core
{
    /// <summary>Descanso: cura 30% da vida máxima ou melhora 1 carta.</summary>
    public static class RestLogic
    {
        public static int HealAmount(RunState r) => (int)Math.Floor(r.MaxHp * r.Config.restHealFraction + 1e-6);

        /// <summary>Devolve quanto curou de fato.</summary>
        public static int Rest(RunState r) => r.Heal(HealAmount(r));

        public static IEnumerable<CardInstance> UpgradableCards(RunState r)
        {
            foreach (var c in r.Deck) if (c.CanUpgrade) yield return c;
        }

        public static bool Upgrade(RunState r, CardInstance card) => r.UpgradeCard(card);
    }

    /// <summary>Baú: 30 moedas e 1 relíquia aleatória que o jogador ainda não tem.</summary>
    public static class ChestLogic
    {
        public sealed class Result
        {
            public int Coins;
            public RelicData Relic;      // null se o jogador já tem todas
        }

        public static Result Open(RunState r)
        {
            var res = new Result { Coins = r.Config.chestCoins };
            r.AddCoins(res.Coins);
            res.Relic = GrantRandomRelic(r);
            return res;
        }

        /// <summary>Sorteia e entrega uma relíquia que falta; null se não falta nenhuma.</summary>
        public static RelicData GrantRandomRelic(RunState r)
        {
            var missing = r.MissingRelics();
            if (missing.Count == 0) return null;
            var relic = missing[r.Rng.Range(0, missing.Count)];
            r.AddRelic(relic);
            return relic;
        }
    }

    public enum ShopItemKind { Card, Relic, Heal, RemoveCard }

    public class ShopItem
    {
        public ShopItemKind Kind;
        public CardData Card;
        public RelicData Relic;
        public int Price;
        public bool Sold;

        public string Label
        {
            get
            {
                switch (Kind)
                {
                    case ShopItemKind.Card: return Card.displayName;
                    case ShopItemKind.Relic: return Relic.displayName;
                    case ShopItemKind.Heal: return "Curar";
                    default: return "Remover carta";
                }
            }
        }
    }

    public enum BuyResult { Ok, NotEnoughCoins, AlreadySold, HpFull, NeedsCardToRemove, DeckTooSmall }

    /// <summary>Loja: 3 cartas, 1 relíquia, cura de 15 e remover 1 carta. Itens comprados somem.</summary>
    public class Shop
    {
        public readonly List<ShopItem> Items = new List<ShopItem>();

        public static Shop Create(RunState r)
        {
            var c = r.Config;
            var shop = new Shop();
            foreach (var card in r.RollCards(c.shopCardCount))
                shop.Items.Add(new ShopItem { Kind = ShopItemKind.Card, Card = card, Price = c.shopCardPrice });

            var missing = r.MissingRelics();
            if (missing.Count > 0)
                shop.Items.Add(new ShopItem { Kind = ShopItemKind.Relic, Relic = missing[r.Rng.Range(0, missing.Count)], Price = c.shopRelicPrice });

            shop.Items.Add(new ShopItem { Kind = ShopItemKind.Heal, Price = c.shopHealPrice });
            shop.Items.Add(new ShopItem { Kind = ShopItemKind.RemoveCard, Price = c.shopRemoveCardPrice });
            return shop;
        }

        /// <summary>Compra um item. Para remover carta, passe a carta escolhida (a interface pergunta antes).</summary>
        public BuyResult Buy(RunState r, ShopItem item, CardInstance cardToRemove = null)
        {
            if (item.Sold) return BuyResult.AlreadySold;
            if (item.Kind == ShopItemKind.Heal && r.Hp >= r.MaxHp) return BuyResult.HpFull;
            if (item.Kind == ShopItemKind.RemoveCard)
            {
                if (r.Deck.Count <= 1) return BuyResult.DeckTooSmall;
                if (cardToRemove == null || !r.Deck.Contains(cardToRemove)) return BuyResult.NeedsCardToRemove;
            }
            if (!r.TrySpend(item.Price)) return BuyResult.NotEnoughCoins;

            switch (item.Kind)
            {
                case ShopItemKind.Card: r.AddCard(item.Card); break;
                case ShopItemKind.Relic: r.AddRelic(item.Relic); break;
                case ShopItemKind.Heal: r.Heal(r.Config.shopHealAmount); break;
                case ShopItemKind.RemoveCard: r.RemoveCard(cardToRemove); break;
            }
            item.Sold = true;
            return BuyResult.Ok;
        }
    }

    /// <summary>Resultado de uma escolha de evento: linhas de texto para mostrar e se falta o jogador escolher uma carta para remover.</summary>
    public class EventResult
    {
        public readonly List<string> Lines = new List<string>();
        public bool RemoveCardPending;
    }

    public static class EventLogic
    {
        public static EventResult Apply(RunState r, EventChoice choice)
        {
            var res = new EventResult();
            foreach (var o in choice.outcomes) ApplyOutcome(r, o, res);
            if (res.Lines.Count == 0) res.Lines.Add("Nada acontece.");
            return res;
        }

        static void ApplyOutcome(RunState r, EventOutcome o, EventResult res)
        {
            switch (o.kind)
            {
                case EventOutcomeKind.Nothing:
                    break;
                case EventOutcomeKind.LoseHp:
                    LoseHp(r, o.value, res);
                    break;
                case EventOutcomeKind.Heal:
                    res.Lines.Add($"Você recupera {r.Heal(o.value)} de vida.");
                    break;
                case EventOutcomeKind.GainCoins:
                    r.AddCoins(o.value);
                    res.Lines.Add($"Você ganha {o.value} moedas.");
                    break;
                case EventOutcomeKind.GainRelic:
                    GainRelic(r, res);
                    break;
                case EventOutcomeKind.GainRandomUpgradedCard:
                    var pool = r.Config.rewardPool;
                    var card = pool[r.Rng.Range(0, pool.Count)];
                    r.AddCard(card, upgraded: true);
                    res.Lines.Add($"Você ganha a carta {card.displayName}+.");
                    break;
                case EventOutcomeKind.RemoveCard:
                    res.RemoveCardPending = true;
                    break;
                case EventOutcomeKind.DiceGamble:
                    int roll = r.Rng.Range(1, r.Config.diceSides + 1);
                    res.Lines.Add($"O dado gigante rola e para no {roll}.");
                    if (roll <= 2) LoseHp(r, o.value, res);
                    else if (roll <= 4) { r.AddCoins(o.value2); res.Lines.Add($"Você ganha {o.value2} moedas."); }
                    else GainRelic(r, res);
                    break;
            }
        }

        /// <summary>Perder vida em evento nunca mata: sobra pelo menos 1.</summary>
        static void LoseHp(RunState r, int amount, EventResult res)
        {
            int lost = Math.Max(0, Math.Min(amount, r.Hp - 1));
            r.LoseHp(lost);
            res.Lines.Add($"Você perde {lost} de vida.");
        }

        static void GainRelic(RunState r, EventResult res)
        {
            var relic = ChestLogic.GrantRandomRelic(r);
            if (relic != null) res.Lines.Add($"Você ganha a relíquia {relic.displayName}.");
            else { r.AddCoins(r.Config.chestCoins); res.Lines.Add($"Você já tem todas as relíquias: ganha {r.Config.chestCoins} moedas."); }
        }
    }
}
