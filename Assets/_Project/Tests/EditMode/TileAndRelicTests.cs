using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using XequeDeTinta.Battle;
using XequeDeTinta.Core;
using XequeDeTinta.Data;

namespace XequeDeTinta.Tests
{
    public class TileAndRelicTests
    {
        static GameConfig Cfg() => AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/_Project/Data/GameConfig.asset");
        static RunState NewRun(int seed = 1) => new RunState(Cfg(), new SeededRng(seed));
        static RelicData Relic(RelicKind k) => Cfg().relics.First(r => r.kind == k);

        static EnemyData Enemy(string name) =>
            AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Project/Data/Enemies/Enemy_" + name + ".asset");

        static BattleSystem Battle(RunState run, params string[] enemies)
        {
            var enc = UnityEngine.ScriptableObject.CreateInstance<EncounterData>();
            enc.enemies = enemies.Select(Enemy).ToList();
            var b = new BattleSystem(new BattleSetup
            {
                HeroHp = run.Hp, HeroMaxHp = run.MaxHp, Deck = new List<CardInstance>(run.Deck), Encounter = enc,
                Rules = BattleRules.From(Cfg()), Rng = new SeededRng(1), Relics = new List<RelicData>(run.Relics),
            });
            b.Begin();
            return b;
        }

        static CardInstance Card(string id) =>
            new CardInstance(Cfg().starterDeck.Concat(Cfg().rewardPool).First(c => c.id == id));

        // ---------- Relíquias ----------

        [Test]
        public void BaseDeChumboDa5DeEscudoNoPrimeiroTurno()
        {
            var run = NewRun(); run.AddRelic(Relic(RelicKind.StartShield));
            var b = Battle(run, "SlimeVerde");
            Assert.AreEqual(5, b.Hero.Shield);
        }

        [Test]
        public void BaseDeChumboProtegeContraOPrimeiroAtaque()
        {
            var run = NewRun(); run.AddRelic(Relic(RelicKind.StartShield));
            var b = Battle(run, "SlimeVermelho");
            b.EndPlayerTurn(); b.RunEnemyTurn();       // ataque 7: 5 absorvidos, 2 passam
            Assert.AreEqual(48, b.Hero.Hp);
        }

        [Test]
        public void BaseDeChumboNaoRepeteNosTurnosSeguintes()
        {
            var run = NewRun(); run.AddRelic(Relic(RelicKind.StartShield));
            var b = Battle(run, "SlimeAzul");
            b.EndPlayerTurn(); b.RunEnemyTurn();
            Assert.AreEqual(0, b.Hero.Shield);
        }

        [Test]
        public void PincelAfiadoComecaComForca1ESomaNoDano()
        {
            var run = NewRun(); run.AddRelic(Relic(RelicKind.StartStrength));
            var b = Battle(run, "SlimeAzul");
            Assert.AreEqual(1, b.Hero.Strength);
            b.Deck.Hand.Clear(); b.Deck.Hand.Add(Card("golpe"));
            b.PlayCard(0, 0).Resolve();
            Assert.AreEqual(22 - 7, b.Enemies[0].Hp);
        }

        [Test]
        public void AmpulhetaDaEnergiaExtraSoNoPrimeiroTurno()
        {
            var run = NewRun(); run.AddRelic(Relic(RelicKind.FirstTurnEnergy));
            var b = Battle(run, "SlimeAzul");
            Assert.AreEqual(4, b.Energy);
            b.EndPlayerTurn(); b.RunEnemyTurn();
            Assert.AreEqual(3, b.Energy);
        }

        [Test]
        public void TintaSecaCura3AoMatarUmInimigo()
        {
            var run = NewRun(); run.AddRelic(Relic(RelicKind.HealOnKill)); run.Hp = 30;
            var b = Battle(run, "SlimeVerde", "SlimeAzul");
            b.Enemies[0].Hp = 1;
            b.Deck.Hand.Clear(); b.Deck.Hand.Add(Card("golpe"));
            var ev = b.PlayCard(0, 0).Resolve();
            Assert.AreEqual(33, b.Hero.Hp);
            Assert.IsTrue(ev.Any(e => e.kind == BattleEventKind.Heal && e.amount == 3));
        }

        [Test]
        public void TintaSecaNaoPassaDaVidaMaxima()
        {
            var run = NewRun(); run.AddRelic(Relic(RelicKind.HealOnKill)); run.Hp = 49;
            var b = Battle(run, "SlimeMirim");
            b.Deck.Hand.Clear(); b.Deck.Hand.Add(Card("golpe_pesado"));
            b.PlayCard(0, 0).Resolve();
            Assert.AreEqual(50, b.Hero.Hp);
        }

        [Test]
        public void SemRelicsNaoHaGanchos()
        {
            var b = Battle(NewRun(), "SlimeVerde");
            Assert.IsEmpty(b.Hooks);
            Assert.AreEqual(3, b.Energy);
        }

        [Test]
        public void DadoViciadoNaoTemGanchoDeBatalha()
        {
            var run = NewRun(); run.AddRelic(Relic(RelicKind.RerollDice));
            Assert.IsEmpty(Battle(run, "SlimeVerde").Hooks);
        }

        [Test]
        public void RelicasSeCombinam()
        {
            var run = NewRun();
            foreach (var r in Cfg().relics) run.AddRelic(r);
            var b = Battle(run, "SlimeAzul");
            Assert.AreEqual(5, b.Hero.Shield);
            Assert.AreEqual(1, b.Hero.Strength);
            Assert.AreEqual(4, b.Energy);
        }

        // ---------- Descanso ----------

        [Test]
        public void DescansoCura30PorCentoDaVidaMaxima()
        {
            var run = NewRun(); run.Hp = 20;
            Assert.AreEqual(15, RestLogic.HealAmount(run));
            Assert.AreEqual(15, RestLogic.Rest(run));
            Assert.AreEqual(35, run.Hp);
        }

        [Test]
        public void DescansoNaoCuraAlemDoMaximo()
        {
            var run = NewRun(); run.Hp = 45;
            Assert.AreEqual(5, RestLogic.Rest(run));
            Assert.AreEqual(50, run.Hp);
        }

        [Test]
        public void DescansoMelhoraUmaCartaUmaVezSo()
        {
            var run = NewRun();
            var c = run.Deck[0];
            Assert.AreEqual(10, RestLogic.UpgradableCards(run).Count());
            Assert.IsTrue(RestLogic.Upgrade(run, c));
            Assert.IsFalse(RestLogic.Upgrade(run, c));
            Assert.AreEqual(9, RestLogic.UpgradableCards(run).Count());
        }

        // ---------- Baú ----------

        [Test]
        public void BauDa30MoedasEUmaRelicaNova()
        {
            var run = NewRun();
            var res = ChestLogic.Open(run);
            Assert.AreEqual(30, run.Coins);
            Assert.IsNotNull(res.Relic);
            Assert.IsTrue(run.Relics.Contains(res.Relic));
        }

        [Test]
        public void BauNuncaRepeteRelicaQueOJogadorTem()
        {
            var run = NewRun();
            for (int i = 0; i < 5; i++) Assert.IsNotNull(ChestLogic.Open(run).Relic);
            Assert.AreEqual(5, run.Relics.Count);
            Assert.IsNull(ChestLogic.Open(run).Relic, "já tem todas: só moedas");
            Assert.AreEqual(180, run.Coins);
        }

        // ---------- Loja ----------

        [Test]
        public void LojaOferece3CartasUmaRelicaCuraERemocao()
        {
            var shop = Shop.Create(NewRun());
            Assert.AreEqual(3, shop.Items.Count(i => i.Kind == ShopItemKind.Card));
            Assert.AreEqual(3, shop.Items.Where(i => i.Kind == ShopItemKind.Card).Select(i => i.Card).Distinct().Count());
            Assert.AreEqual(1, shop.Items.Count(i => i.Kind == ShopItemKind.Relic));
            Assert.AreEqual(1, shop.Items.Count(i => i.Kind == ShopItemKind.Heal));
            Assert.AreEqual(1, shop.Items.Count(i => i.Kind == ShopItemKind.RemoveCard));
        }

        [Test]
        public void PrecosDaLojaSaoOsDoGdd()
        {
            var shop = Shop.Create(NewRun());
            Assert.IsTrue(shop.Items.Where(i => i.Kind == ShopItemKind.Card).All(i => i.Price == 50));
            Assert.AreEqual(100, shop.Items.First(i => i.Kind == ShopItemKind.Relic).Price);
            Assert.AreEqual(35, shop.Items.First(i => i.Kind == ShopItemKind.Heal).Price);
            Assert.AreEqual(60, shop.Items.First(i => i.Kind == ShopItemKind.RemoveCard).Price);
        }

        [Test]
        public void ComprarCartaDebitaMoedasEAdicionaAoDeck()
        {
            var run = NewRun(); run.AddCoins(120);
            var shop = Shop.Create(run);
            var item = shop.Items.First(i => i.Kind == ShopItemKind.Card);
            Assert.AreEqual(BuyResult.Ok, shop.Buy(run, item));
            Assert.AreEqual(70, run.Coins);
            Assert.AreEqual(11, run.Deck.Count);
            Assert.IsTrue(item.Sold);
        }

        [Test]
        public void ItemComprado_SomeEnaoCompraDeNovo()
        {
            var run = NewRun(); run.AddCoins(200);
            var shop = Shop.Create(run);
            var item = shop.Items.First(i => i.Kind == ShopItemKind.Card);
            shop.Buy(run, item);
            Assert.AreEqual(BuyResult.AlreadySold, shop.Buy(run, item));
            Assert.AreEqual(150, run.Coins);
        }

        [Test]
        public void SemMoedasNaoCompra()
        {
            var run = NewRun(); run.AddCoins(49);
            var shop = Shop.Create(run);
            Assert.AreEqual(BuyResult.NotEnoughCoins, shop.Buy(run, shop.Items.First(i => i.Kind == ShopItemKind.Card)));
            Assert.AreEqual(49, run.Coins);
            Assert.AreEqual(10, run.Deck.Count);
        }

        [Test]
        public void ComprarRelicaDa100DeMoedas()
        {
            var run = NewRun(); run.AddCoins(100);
            var shop = Shop.Create(run);
            var item = shop.Items.First(i => i.Kind == ShopItemKind.Relic);
            Assert.AreEqual(BuyResult.Ok, shop.Buy(run, item));
            Assert.AreEqual(0, run.Coins);
            Assert.IsTrue(run.Relics.Contains(item.Relic));
        }

        [Test]
        public void CuraDaLojaRecupera15EBloqueiaComVidaCheia()
        {
            var run = NewRun(); run.AddCoins(100);
            var shop = Shop.Create(run);
            var heal = shop.Items.First(i => i.Kind == ShopItemKind.Heal);
            Assert.AreEqual(BuyResult.HpFull, shop.Buy(run, heal));
            Assert.AreEqual(100, run.Coins, "não cobra se não pode curar");
            run.Hp = 20;
            Assert.AreEqual(BuyResult.Ok, shop.Buy(run, heal));
            Assert.AreEqual(35, run.Hp);
            Assert.AreEqual(65, run.Coins);
        }

        [Test]
        public void RemoverCartaExigeEscolherACartaECobra60()
        {
            var run = NewRun(); run.AddCoins(70);
            var shop = Shop.Create(run);
            var rm = shop.Items.First(i => i.Kind == ShopItemKind.RemoveCard);
            Assert.AreEqual(BuyResult.NeedsCardToRemove, shop.Buy(run, rm));
            Assert.AreEqual(70, run.Coins);
            var alvo = run.Deck.First(c => c.Data.id == "defender");
            Assert.AreEqual(BuyResult.Ok, shop.Buy(run, rm, alvo));
            Assert.AreEqual(10, run.Coins);
            Assert.AreEqual(9, run.Deck.Count);
            Assert.IsFalse(run.Deck.Contains(alvo));
        }

        [Test]
        public void LojaSemRelicaQuandoOJogadorTemTodas()
        {
            var run = NewRun();
            foreach (var r in Cfg().relics) run.AddRelic(r);
            Assert.AreEqual(0, Shop.Create(run).Items.Count(i => i.Kind == ShopItemKind.Relic));
        }

        // ---------- Eventos ----------

        static EventData Ev(string id) => Cfg().events.First(e => e.id == id);

        [Test]
        public void PocaDeTintaBeberPerde6EGanhaCartaMelhorada()
        {
            var run = NewRun();
            var res = EventLogic.Apply(run, Ev("poca_de_tinta").choiceA);
            Assert.AreEqual(44, run.Hp);
            Assert.AreEqual(11, run.Deck.Count);
            Assert.IsTrue(run.Deck.Last().Upgraded);
            Assert.IsTrue(Cfg().rewardPool.Contains(run.Deck.Last().Data));
            Assert.AreEqual(2, res.Lines.Count);
        }

        [Test]
        public void PocaDeTintaSeguirEmFrenteNaoMudaNada()
        {
            var run = NewRun();
            EventLogic.Apply(run, Ev("poca_de_tinta").choiceB);
            Assert.AreEqual(50, run.Hp); Assert.AreEqual(10, run.Deck.Count);
        }

        [Test]
        public void EventoNuncaMataOHeroi()
        {
            var run = NewRun(); run.Hp = 4;
            EventLogic.Apply(run, Ev("poca_de_tinta").choiceA);
            Assert.AreEqual(1, run.Hp);
        }

        [TestCase(1, 8, 0, 0)]
        [TestCase(2, 8, 0, 0)]
        [TestCase(3, 0, 30, 0)]
        [TestCase(4, 0, 30, 0)]
        [TestCase(5, 0, 0, 1)]
        [TestCase(6, 0, 0, 1)]
        public void DadoGiganteSegueATabela(int roll, int hpLost, int coins, int relics)
        {
            var run = NewRun();
            run.Hp = 40;
            run.Rng = new ScriptedRng(roll);                  // dita a rolagem do dado gigante
            EventLogic.Apply(run, Ev("dado_gigante").choiceA);
            Assert.AreEqual(40 - hpLost, run.Hp);
            Assert.AreEqual(coins, run.Coins);
            Assert.AreEqual(relics, run.Relics.Count);
        }

        [Test]
        public void FonteDeFeltroCura12()
        {
            var run = NewRun(); run.Hp = 20;
            EventLogic.Apply(run, Ev("fonte_de_feltro").choiceA);
            Assert.AreEqual(32, run.Hp);
        }

        [Test]
        public void FonteDeFeltroRemoverPedeUmaCarta()
        {
            var run = NewRun();
            var res = EventLogic.Apply(run, Ev("fonte_de_feltro").choiceB);
            Assert.IsTrue(res.RemoveCardPending);
            Assert.AreEqual(10, run.Deck.Count, "a remoção só acontece depois da escolha");
        }
    }
}
