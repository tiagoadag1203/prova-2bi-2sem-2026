using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using XequeDeTinta.Data;

namespace XequeDeTinta.Tests
{
    /// <summary>Confere os assets de dados contra as tabelas do GDD (seções 3 a 8).</summary>
    public class DataAssetTests
    {
        static GameConfig Config() =>
            AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/_Project/Data/GameConfig.asset");

        [Test]
        public void ConfigExisteComNumerosDoGdd()
        {
            var c = Config();
            Assert.IsNotNull(c, "GameConfig.asset não existe (rode Xeque > M1 > Criar assets de dados)");
            Assert.AreEqual(50, c.heroMaxHp);
            Assert.AreEqual(3, c.energyPerTurn);
            Assert.AreEqual(5, c.handSize);
            Assert.AreEqual(1.5f, c.effectiveMultiplier);
            Assert.AreEqual(0.75f, c.weakMultiplier);
            Assert.AreEqual(2, c.burnDamage);
            Assert.AreEqual(0.3f, c.restHealFraction);
        }

        [Test]
        public void DeckInicialTem5Golpe4Defender1Investida()
        {
            var deck = Config().starterDeck;
            Assert.AreEqual(10, deck.Count);
            Assert.AreEqual(5, deck.Count(x => x.id == "golpe"));
            Assert.AreEqual(4, deck.Count(x => x.id == "defender"));
            Assert.AreEqual(1, deck.Count(x => x.id == "investida"));
        }

        [Test]
        public void PoolDeRecompensaTem9CartasDistintas()
        {
            var pool = Config().rewardPool;
            Assert.AreEqual(9, pool.Count);
            Assert.AreEqual(9, pool.Select(x => x.id).Distinct().Count());
            Assert.IsTrue(pool.All(x => x.inRewardPool));
        }

        [Test]
        public void TodaCartaTemVersaoMelhoradaDoMesmoTamanho()
        {
            var cards = AssetDatabase.FindAssets("t:CardData", new[] { "Assets/_Project/Data/Cards" })
                .Select(g => AssetDatabase.LoadAssetAtPath<CardData>(AssetDatabase.GUIDToAssetPath(g))).ToList();
            Assert.AreEqual(12, cards.Count);
            foreach (var c in cards)
            {
                Assert.IsNotEmpty(c.effects, c.id);
                Assert.AreEqual(c.effects.Count, c.upgradedEffects.Count, c.id);
                Assert.IsNotEmpty(c.description, c.id);
                Assert.IsNotEmpty(c.upgradedDescription, c.id);
            }
        }

        static CardData Card(string id) => Config().starterDeck.Concat(Config().rewardPool).First(c => c.id == id);

        [TestCase("golpe", 1, 6, 9)]
        [TestCase("defender", 1, 5, 8)]
        [TestCase("investida", 2, 10, 14)]
        [TestCase("bola_de_fogo", 1, 7, 10)]
        [TestCase("onda", 2, 5, 8)]
        [TestCase("golpe_pesado", 2, 14, 18)]
        public void CartasSimplesTemCustoEValoresDoGdd(string id, int cost, int value, int upgraded)
        {
            var c = Card(id);
            Assert.AreEqual(cost, c.cost);
            Assert.AreEqual(value, c.effects[0].value);
            Assert.AreEqual(upgraded, c.upgradedEffects[0].value);
        }

        [Test]
        public void CartasEspeciais()
        {
            var duplo = Card("golpe_duplo");
            Assert.AreEqual(2, duplo.effects[0].hits);
            Assert.AreEqual(4, duplo.effects[0].value);
            Assert.AreEqual(5, duplo.upgradedEffects[0].value);

            var seiva = Card("seiva");
            Assert.IsTrue(seiva.exhaust);
            Assert.AreEqual(EffectKind.Heal, seiva.effects[0].kind);
            Assert.AreEqual(9, seiva.upgradedEffects[0].value);

            Assert.AreEqual(CardTarget.AllEnemies, Card("onda").target);
            Assert.AreEqual(Element.Water, Card("onda").element);
            Assert.AreEqual(EffectKind.Stun, Card("investida").effects[1].kind);

            var respingo = Card("respingo");
            Assert.AreEqual(1, respingo.effects[1].value);
            Assert.AreEqual(2, respingo.upgradedEffects[1].value);

            Assert.AreEqual(3, Card("brasa").effects[0].value);
            Assert.AreEqual(5, Card("brasa").upgradedEffects[0].value);
            Assert.AreEqual(Element.Fire, Card("brasa").element);

            var raiz = Card("raiz");
            Assert.AreEqual(4, raiz.effects[0].value); Assert.AreEqual(4, raiz.effects[1].value);
            Assert.AreEqual(6, raiz.upgradedEffects[0].value); Assert.AreEqual(6, raiz.upgradedEffects[1].value);
        }

        static EnemyData Enemy(string id) =>
            AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Project/Data/Enemies/Enemy_" +
                string.Concat(id.Split('_').Select(p => char.ToUpper(p[0]) + p.Substring(1))) + ".asset");

        [Test]
        public void InimigosTemVidaEElementoDoGdd()
        {
            Assert.AreEqual((18, Element.Plant), (Enemy("slime_verde").maxHp, Enemy("slime_verde").element));
            Assert.AreEqual((14, Element.Fire), (Enemy("slime_vermelho").maxHp, Enemy("slime_vermelho").element));
            Assert.AreEqual((22, Element.Water), (Enemy("slime_azul").maxHp, Enemy("slime_azul").element));
            Assert.AreEqual((70, Element.Plant), (Enemy("rei_slime").maxHp, Enemy("rei_slime").element));
            Assert.AreEqual((8, Element.Plant), (Enemy("slime_mirim").maxHp, Enemy("slime_mirim").element));
        }

        [Test]
        public void ComportamentoDosInimigos()
        {
            var verde = Enemy("slime_verde");
            Assert.AreEqual(5, verde.actions[0].damage);
            Assert.AreEqual(4, verde.actions[1].shield);

            var verm = Enemy("slime_vermelho");
            Assert.AreEqual(7, verm.actions[0].damage);
            Assert.AreEqual(2, verm.actions[1].burnTurns);
            Assert.AreEqual(3, verm.specialEveryN);

            var azul = Enemy("slime_azul");
            Assert.AreEqual(4, azul.actions[0].damage);
            Assert.AreEqual(3, azul.actions[0].heal);

            var rei = Enemy("rei_slime");
            Assert.IsTrue(rei.isBoss);
            Assert.AreEqual(12, rei.actions[0].damage);
            Assert.AreEqual(2, rei.actions[1].summonCount);
            Assert.AreEqual(8, rei.actions[2].shield);
            Assert.AreEqual(6, rei.actions[2].damage);
            Assert.AreEqual(0.5f, rei.enrageBelowHpFraction);
            Assert.AreEqual(2, rei.enrageStrength);
            Assert.AreEqual(Enemy("slime_mirim"), rei.summonedEnemy);
            Assert.AreEqual(2, rei.summonMaxAlive);

            var mirim = Enemy("slime_mirim");
            Assert.AreEqual(3, mirim.actions[0].damage);
            Assert.AreEqual(0.6f, mirim.spriteScale);
        }

        [Test]
        public void CincoEncontrosComosGruposDoGdd()
        {
            var enc = Config().battleEncounters;
            Assert.AreEqual(5, enc.Count);
            string[][] expected =
            {
                new[] { "slime_verde" }, new[] { "slime_vermelho" }, new[] { "slime_verde", "slime_azul" },
                new[] { "slime_vermelho", "slime_verde" }, new[] { "slime_azul", "slime_vermelho", "slime_verde" },
            };
            for (int i = 0; i < 5; i++)
                CollectionAssert.AreEqual(expected[i], enc[i].enemies.Select(e => e.id).ToArray(), "encontro " + i);
            Assert.IsTrue(Config().bossEncounter.isBoss);
            Assert.AreEqual("rei_slime", Config().bossEncounter.enemies[0].id);
        }

        [Test]
        public void CincoReliquias()
        {
            var r = Config().relics;
            Assert.AreEqual(5, r.Count);
            CollectionAssert.AreEquivalent(
                new[] { RelicKind.RerollDice, RelicKind.StartShield, RelicKind.HealOnKill, RelicKind.StartStrength, RelicKind.FirstTurnEnergy },
                r.Select(x => x.kind).ToArray());
            Assert.AreEqual(5, r.First(x => x.kind == RelicKind.StartShield).value);
            Assert.AreEqual(3, r.First(x => x.kind == RelicKind.HealOnKill).value);
        }

        [Test]
        public void TresEventosComDuasEscolhas()
        {
            var ev = Config().events;
            Assert.AreEqual(3, ev.Count);
            foreach (var e in ev)
            {
                Assert.IsNotEmpty(e.title); Assert.IsNotEmpty(e.body);
                Assert.IsNotEmpty(e.choiceA.text); Assert.IsNotEmpty(e.choiceB.text);
                Assert.IsNotEmpty(e.choiceA.outcomes); Assert.IsNotEmpty(e.choiceB.outcomes);
            }
        }

        [Test]
        public void TabuleiroTem12CasasComAsQuantidadesDoGdd()
        {
            var b = Config().board;
            Assert.AreEqual(12, b.tiles.Count);
            int Count(TileType t) => b.tiles.Count(x => x.type == t);
            Assert.AreEqual(1, Count(TileType.Start));
            Assert.AreEqual(5, Count(TileType.Battle));
            Assert.AreEqual(1, Count(TileType.Shop));
            Assert.AreEqual(1, Count(TileType.Rest));
            Assert.AreEqual(2, Count(TileType.Event));
            Assert.AreEqual(1, Count(TileType.Chest));
            Assert.AreEqual(1, Count(TileType.Boss));
        }

        [Test]
        public void TabuleiroTem2BifurcacoesQueSeJuntamEChegaAoChefe()
        {
            var b = Config().board;
            Assert.AreEqual(2, b.tiles.Count(t => t.next.Count == 2), "bifurcações");
            Assert.IsTrue(b.tiles.All(t => t.next.Count <= 2));
            foreach (var t in b.tiles.Where(t => t.next.Count == 2))
            {
                var ends = t.next.Select(n => b.Get(n).next.Single()).Distinct().ToList();
                Assert.AreEqual(1, ends.Count, "ramos da casa " + t.id + " devem se juntar");
            }
            // Toda casa alcança o Chefe; o Chefe não tem saída.
            var boss = b.tiles.Single(t => t.type == TileType.Boss);
            Assert.IsEmpty(boss.next);
            foreach (var t in b.tiles) Assert.IsTrue(Reaches(b, t.id, boss.id), "casa " + t.id);
            // Ids únicos e todas as ligações apontam para casas existentes.
            Assert.AreEqual(12, b.tiles.Select(t => t.id).Distinct().Count());
            foreach (var t in b.tiles) foreach (var n in t.next) Assert.IsNotNull(b.Get(n));
        }

        static bool Reaches(BoardLayout b, int from, int to)
        {
            var seen = new HashSet<int>(); var stack = new Stack<int>(); stack.Push(from);
            while (stack.Count > 0)
            {
                var id = stack.Pop();
                if (id == to) return true;
                if (!seen.Add(id)) continue;
                foreach (var n in b.Get(id).next) stack.Push(n);
            }
            return false;
        }

        [Test]
        public void LojaERecompensasUsamValoresDoGdd()
        {
            var c = Config();
            Assert.AreEqual((10, 15), (c.rewardCoinsMin, c.rewardCoinsMax));
            Assert.AreEqual(3, c.rewardCardChoices);
            Assert.AreEqual(30, c.chestCoins);
            Assert.AreEqual((3, 50, 100), (c.shopCardCount, c.shopCardPrice, c.shopRelicPrice));
            Assert.AreEqual((15, 35, 60), (c.shopHealAmount, c.shopHealPrice, c.shopRemoveCardPrice));
        }
    }
}
