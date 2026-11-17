using System.Linq;
using NUnit.Framework;
using UnityEditor;
using XequeDeTinta.Core;
using XequeDeTinta.Data;

namespace XequeDeTinta.Tests
{
    public class RunStateTests
    {
        static GameConfig Cfg() => AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/_Project/Data/GameConfig.asset");
        static RunState NewRun(int seed = 1) => new RunState(Cfg(), new SeededRng(seed));

        [Test]
        public void PartidaComecaComVida50DeckInicialEZeroMoedas()
        {
            var r = NewRun();
            Assert.AreEqual(50, r.Hp); Assert.AreEqual(50, r.MaxHp);
            Assert.AreEqual(0, r.Coins);
            Assert.AreEqual(10, r.Deck.Count);
            Assert.AreEqual(5, r.Deck.Count(c => c.Data.id == "golpe"));
            Assert.IsEmpty(r.Relics);
            Assert.AreEqual(0, r.Progress.CurrentTile);
            Assert.IsEmpty(r.Progress.Visited);
        }

        [Test]
        public void AsCincoBatalhasSaemSemRepetirNemFaltar()
        {
            var r = NewRun(7);
            var seen = Enumerable.Range(0, 5).Select(_ => r.NextBattle().id).ToList();
            CollectionAssert.AreEquivalent(Cfg().battleEncounters.Select(e => e.id), seen);
        }

        [Test]
        public void OrdemDasBatalhasDependeDaSemente()
        {
            string Order(int seed) { var r = NewRun(seed); return string.Join(",", Enumerable.Range(0, 5).Select(_ => r.NextBattle().id)); }
            Assert.AreEqual(Order(3), Order(3));
            Assert.AreNotEqual(Order(3), Order(4), "sementes diferentes costumam dar ordens diferentes");
        }

        [Test]
        public void DuasCasasDeEventoSorteiamDoisDosTresSemRepetir()
        {
            for (int seed = 0; seed < 30; seed++)
            {
                var r = NewRun(seed);
                var a = r.NextEvent(); var b = r.NextEvent();
                Assert.AreNotSame(a, b);
                Assert.IsTrue(Cfg().events.Contains(a) && Cfg().events.Contains(b));
            }
        }

        [Test]
        public void RecompensaDeMoedasFicaEntre10e15()
        {
            var r = NewRun(2);
            for (int i = 0; i < 300; i++) Assert.That(r.RollCoins(), Is.InRange(10, 15));
        }

        [Test]
        public void RecompensaOferece3CartasDistintasDoPool()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var cards = NewRun(seed).RollCards(3);
                Assert.AreEqual(3, cards.Count);
                Assert.AreEqual(3, cards.Distinct().Count());
                Assert.IsTrue(cards.All(c => Cfg().rewardPool.Contains(c)));
            }
        }

        [Test]
        public void TodasAsCartasDoPoolPodemAparecerNaRecompensa()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            var r = NewRun(11);
            for (int i = 0; i < 200; i++) foreach (var c in r.RollCards(3)) seen.Add(c.id);
            Assert.AreEqual(9, seen.Count);
        }

        [Test]
        public void CurarNaoPassaDoMaximoEDanoNaoPassaDeZero()
        {
            var r = NewRun();
            r.Hp = 40;
            Assert.AreEqual(10, r.Heal(30));
            Assert.AreEqual(50, r.Hp);
            r.LoseHp(999);
            Assert.AreEqual(0, r.Hp);
            Assert.IsFalse(r.IsAlive);
        }

        [Test]
        public void GastarMoedasSoComSaldo()
        {
            var r = NewRun();
            r.AddCoins(40);
            Assert.IsFalse(r.TrySpend(50));
            Assert.AreEqual(40, r.Coins);
            Assert.IsTrue(r.TrySpend(35));
            Assert.AreEqual(5, r.Coins);
        }

        [Test]
        public void AdicionarRemoverEMelhorarCartas()
        {
            var r = NewRun();
            var golpe = r.Deck.First(c => c.Data.id == "golpe");
            Assert.IsTrue(r.UpgradeCard(golpe));
            Assert.IsFalse(r.UpgradeCard(golpe), "já melhorada");
            var nova = r.AddCard(Cfg().rewardPool[0]);
            Assert.AreEqual(11, r.Deck.Count);
            Assert.IsTrue(r.RemoveCard(nova));
            Assert.AreEqual(10, r.Deck.Count);
            Assert.IsFalse(r.RemoveCard(nova));
        }

        [Test]
        public void RelíquiasNaoDuplicamEListamAsQueFaltam()
        {
            var r = NewRun();
            var rel = Cfg().relics[0];
            r.AddRelic(rel); r.AddRelic(rel);
            Assert.AreEqual(1, r.Relics.Count);
            Assert.IsTrue(r.HasRelic(rel.kind));
            Assert.AreEqual(4, r.MissingRelics().Count);
        }
    }
}
