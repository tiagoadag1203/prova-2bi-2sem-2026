using System;
using System.Collections.Generic;
using System.Linq;
using XequeDeTinta.Battle;
using XequeDeTinta.Board;
using XequeDeTinta.Core;
using XequeDeTinta.Data;

namespace XequeDeTinta.Bot
{
    /// <summary>
    /// Política de um jogador razoável (não perfeito): usada no playtest automático e na simulação de balanceamento.
    /// Só decide; quem executa é o simulador (lógica pura) ou o teste de PlayMode (cenas de verdade).
    /// </summary>
    public static class BotPolicy
    {
        // ---------- Batalha ----------

        public struct Move { public int handIndex, targetIndex; public float score; }

        /// <summary>Melhor jogada agora, ou null se não vale jogar mais nada.</summary>
        public static Move? ChooseCard(BattleSystem b)
        {
            var hand = b.Deck.Hand;
            int incoming = 0;
            foreach (var e in b.AliveEnemies)
                if (!e.Stunned) incoming += b.PreviewDamage(e);
            int missingBlock = Math.Max(0, incoming - b.Hero.Shield);
            bool hurt = b.Hero.Hp < b.Hero.MaxHp;

            Move? best = null;
            for (int i = 0; i < hand.Count; i++)
            {
                if (!b.CanPlay(i)) continue;
                var card = hand[i];
                var targets = new List<int>();
                if (card.Data.target == CardTarget.SingleEnemy)
                    for (int t = 0; t < b.Enemies.Count; t++) { if (b.Enemies[t].IsAlive) targets.Add(t); }
                else targets.Add(-1);

                foreach (int t in targets)
                {
                    float score = Score(b, card, t, missingBlock, hurt) / Math.Max(1, card.Cost);
                    if (score > 0.01f && (best == null || score > best.Value.score))
                        best = new Move { handIndex = i, targetIndex = t, score = score };
                }
            }
            return best;
        }

        static float Score(BattleSystem b, CardInstance card, int target, int missingBlock, bool hurt)
        {
            float s = 0f;
            var rules = b.Rules;
            var targets = new List<Enemy>();
            if (card.Data.target == CardTarget.SingleEnemy) targets.Add(b.Enemies[target]);
            else if (card.Data.target == CardTarget.AllEnemies) targets.AddRange(b.AliveEnemies);

            foreach (var spec in card.Effects)
            {
                switch (spec.kind)
                {
                    case EffectKind.DealDamage:
                        foreach (var e in targets)
                        {
                            int total = 0;
                            int pool = e.Hp + e.Shield;
                            for (int h = 0; h < Math.Max(1, spec.hits); h++)
                                total += DamageCalc.Calculate(spec.value, card.Element, b.Hero.Strength, b.Hero.Weak > 0, e.Element, rules).amount;
                            float v = Math.Min(total, pool);
                            if (total >= pool) v += 8f;                       // abate o inimigo
                            if (e.Intent != null && e.Intent.damage >= 7) v += 1.5f;   // inimigo perigoso
                            s += v;
                        }
                        break;
                    case EffectKind.GainShield:
                        s += Math.Min(spec.value, missingBlock) * 1.1f + (missingBlock == 0 ? 0.2f : 0f);
                        break;
                    case EffectKind.Heal:
                        s += hurt ? Math.Min(spec.value, b.Hero.MaxHp - b.Hero.Hp) * (b.Hero.Hp < b.Hero.MaxHp * 0.6f ? 1.2f : 0.7f) : 0f;
                        break;
                    case EffectKind.ApplyBurn:
                        foreach (var e in targets) s += Math.Min(spec.value, 4) * 1.6f;
                        break;
                    case EffectKind.ApplyWeak:
                        foreach (var e in targets) s += e.Intent != null && e.Intent.damage > 0 ? 2.5f : 0.5f;
                        break;
                    case EffectKind.Stun:
                        foreach (var e in targets)
                            s += e.Intent != null && e.Intent.damage >= 7 ? 7f : (e.Intent != null && e.Intent.damage > 0 ? 3f : 1f);
                        break;
                }
            }
            return s;
        }

        // ---------- Recompensa, loja, descanso, evento, bifurcação ----------

        static readonly string[] CardPriority =
            { "golpe_pesado", "bola_de_fogo", "onda", "raiz", "casca_grossa", "golpe_duplo", "respingo", "brasa", "seiva" };

        static int Priority(string id) { int i = Array.IndexOf(CardPriority, id); return i < 0 ? 99 : i; }

        /// <summary>Índice da carta a levar, ou -1 para pular.</summary>
        public static int ChooseReward(RunState run, IList<CardData> choices)
        {
            if (run.Deck.Count >= 18) return -1;
            int best = -1, bestP = 99;
            for (int i = 0; i < choices.Count; i++)
            {
                int p = Priority(choices[i].id);
                if (p < bestP) { bestP = p; best = i; }
            }
            return best;
        }

        public static void UseRest(RunState run)
        {
            if (run.Hp <= run.MaxHp * 0.65f || !RestLogic.UpgradableCards(run).Any()) { RestLogic.Rest(run); return; }
            var target = RestLogic.UpgradableCards(run).OrderBy(c => Priority(c.Data.id)).ThenByDescending(c => c.Data.id == "golpe" ? 1 : 0).First();
            RestLogic.Upgrade(run, target);
        }

        public static void UseShop(RunState run, Shop shop)
        {
            foreach (var item in shop.Items.Where(i => i.Kind == ShopItemKind.Relic)) shop.Buy(run, item);
            if (run.Hp < run.MaxHp * 0.6f) foreach (var item in shop.Items.Where(i => i.Kind == ShopItemKind.Heal)) shop.Buy(run, item);
            foreach (var item in shop.Items.Where(i => i.Kind == ShopItemKind.Card).OrderBy(i => Priority(i.Card.id)))
                if (run.Deck.Count < 18) shop.Buy(run, item);
            var rm = shop.Items.FirstOrDefault(i => i.Kind == ShopItemKind.RemoveCard);
            if (rm != null && run.Coins >= rm.Price)
            {
                var weakest = run.Deck.FirstOrDefault(c => c.Data.id == "defender" && c.Upgraded == false);
                if (weakest != null && run.Deck.Count(c => c.Data.id == "defender") > 2) shop.Buy(run, rm, weakest);
            }
        }

        /// <summary>true = escolha A, false = escolha B.</summary>
        public static bool ChooseEventA(RunState run, EventData ev)
        {
            switch (ev.id)
            {
                case "poca_de_tinta": return run.Hp > 28;
                case "dado_gigante": return run.Hp > 20;
                default: return run.Hp < run.MaxHp - 12;
            }
        }

        /// <summary>Se o evento pediu para remover uma carta, qual.</summary>
        public static CardInstance CardToRemove(RunState run) =>
            run.Deck.FirstOrDefault(c => c.Data.id == "defender" && !c.Upgraded) ?? run.Deck.First();

        public static int ChooseBranch(BoardGraph graph, IReadOnlyList<int> options, RunState run, IRng rng)
        {
            int Rank(int id)
            {
                var t = graph.TypeOf(id);
                if (t == TileType.Rest) return run.Hp < run.MaxHp * 0.75f ? 0 : 4;
                if (t == TileType.Shop) return run.Coins >= 50 ? 1 : 5;
                if (t == TileType.Chest) return 2;
                return 3 + rng.Range(0, 2);
            }
            return options.OrderBy(Rank).First();
        }
    }
}
