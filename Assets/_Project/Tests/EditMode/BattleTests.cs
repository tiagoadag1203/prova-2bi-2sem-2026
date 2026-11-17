using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using XequeDeTinta.Battle;
using XequeDeTinta.Core;
using XequeDeTinta.Data;

namespace XequeDeTinta.Tests
{
    public class BattleTests
    {
        static GameConfig Cfg() => AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/_Project/Data/GameConfig.asset");

        static CardData CardData(string id) =>
            Cfg().starterDeck.Concat(Cfg().rewardPool).First(c => c.id == id);

        static CardInstance Card(string id, bool up = false) => new CardInstance(CardData(id), up);

        static EnemyData EnemyData(string id) =>
            AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Project/Data/Enemies/Enemy_" +
                string.Concat(id.Split('_').Select(p => char.ToUpper(p[0]) + p.Substring(1))) + ".asset");

        static EncounterData Encounter(params string[] enemies)
        {
            var e = UnityEngine.ScriptableObject.CreateInstance<EncounterData>();
            e.enemies = enemies.Select(EnemyData).ToList();
            return e;
        }

        static BattleSystem NewBattle(string[] enemies, int seed = 1, int heroHp = 50, List<CardInstance> deck = null, IRng rng = null)
        {
            var setup = new BattleSetup
            {
                HeroHp = heroHp, HeroMaxHp = 50,
                Deck = deck ?? Cfg().starterDeck.Select(c => new CardInstance(c)).ToList(),
                Encounter = Encounter(enemies),
                Rules = BattleRules.From(Cfg()),
                Rng = rng ?? new SeededRng(seed),
            };
            var b = new BattleSystem(setup);
            b.Begin();
            return b;
        }

        static void Hand(BattleSystem b, params CardInstance[] cards)
        {
            b.Deck.Hand.Clear();
            b.Deck.Hand.AddRange(cards);
        }

        static List<BattleEvent> Play(BattleSystem b, int handIndex, int target = 0) => b.PlayCard(handIndex, target).Resolve();

        // ---------- Dano ----------

        static readonly BattleRules R = new BattleRules();

        [Test]
        public void DanoBaseSemModificadores() =>
            Assert.AreEqual(6, DamageCalc.Calculate(6, Element.Neutral, 0, false, Element.Plant, R).amount);

        [Test]
        public void ForcaSomaAntesDoMultiplicador() =>
            Assert.AreEqual(12, DamageCalc.Calculate(6, Element.Fire, 2, false, Element.Plant, R).amount); // (6+2)*1,5

        [Test]
        public void EficazMultiplica1e5EArredondaParaBaixo()
        {
            var r = DamageCalc.Calculate(7, Element.Fire, 0, false, Element.Plant, R);
            Assert.AreEqual(10, r.amount); // 10,5 -> 10
            Assert.IsTrue(r.effective);
        }

        [Test]
        public void FracoReduz25PorCentoEArredondaParaBaixo() =>
            Assert.AreEqual(5, DamageCalc.Calculate(7, Element.Neutral, 0, true, Element.Neutral, R).amount); // 5,25 -> 5

        [Test]
        public void EficazEFracoJuntos() =>
            Assert.AreEqual(4, DamageCalc.Calculate(4, Element.Water, 0, true, Element.Fire, R).amount); // 4*1,5*0,75=4,5 -> 4

        [TestCase(Element.Fire, Element.Plant, true)]
        [TestCase(Element.Plant, Element.Water, true)]
        [TestCase(Element.Water, Element.Fire, true)]
        [TestCase(Element.Plant, Element.Fire, false)]
        [TestCase(Element.Water, Element.Plant, false)]
        [TestCase(Element.Fire, Element.Water, false)]
        [TestCase(Element.Neutral, Element.Plant, false)]
        [TestCase(Element.Fire, Element.Neutral, false)]
        [TestCase(Element.Fire, Element.Fire, false)]
        public void TabelaDeElementos(Element atk, Element def, bool expected) =>
            Assert.AreEqual(expected, ElementRules.Beats(atk, def));

        [Test]
        public void EscudoAbsorvePrimeiro()
        {
            var c = new Combatant { Hp = 20, MaxHp = 20, Shield = 5 };
            int lost = c.TakeDamage(8, out int absorbed);
            Assert.AreEqual(3, lost); Assert.AreEqual(5, absorbed);
            Assert.AreEqual(17, c.Hp); Assert.AreEqual(0, c.Shield);
        }

        [Test]
        public void EscudoMaiorQueODanoSobra()
        {
            var c = new Combatant { Hp = 20, MaxHp = 20, Shield = 9 };
            c.TakeDamage(4, out _);
            Assert.AreEqual(20, c.Hp); Assert.AreEqual(5, c.Shield);
        }

        [Test]
        public void CuraNaoPassaDaVidaMaxima()
        {
            var c = new Combatant { Hp = 45, MaxHp = 50 };
            Assert.AreEqual(5, c.Heal(9));
            Assert.AreEqual(50, c.Hp);
        }

        // ---------- Início, compra e energia ----------

        [Test]
        public void ComecaNoTurnoDoJogadorCom5CartasE3DeEnergia()
        {
            var b = NewBattle(new[] { "slime_verde" });
            Assert.AreEqual(BattlePhase.PlayerTurn, b.Phase);
            Assert.AreEqual(5, b.Deck.Hand.Count);
            Assert.AreEqual(5, b.Deck.DrawPile.Count);
            Assert.AreEqual(3, b.Energy);
        }

        [Test]
        public void CartaCustaEnergiaESaiDaMao()
        {
            var b = NewBattle(new[] { "slime_verde" });
            Hand(b, Card("golpe"), Card("investida"));
            Play(b, 0);
            Assert.AreEqual(2, b.Energy);
            Assert.AreEqual(1, b.Deck.Hand.Count);
            Assert.AreEqual(1, b.Deck.DiscardPile.Count);
        }

        [Test]
        public void NaoJogaSemEnergia()
        {
            var b = NewBattle(new[] { "slime_azul" });
            Hand(b, Card("golpe_pesado"), Card("golpe_pesado"));
            Play(b, 0);                       // gasta 2, sobra 1
            Assert.IsFalse(b.CanPlay(0));     // custa 2
            Assert.Throws<System.InvalidOperationException>(() => b.PlayCard(0, 0));
        }

        [Test]
        public void CartaDeAlvoUnicoExigeAlvoVivo()
        {
            var b = NewBattle(new[] { "slime_verde" });
            Hand(b, Card("golpe"));
            Assert.Throws<System.InvalidOperationException>(() => b.PlayCard(0, -1));
        }

        [Test]
        public void EncerrarTurnoDescartaMaoEEnergiaVoltaA3()
        {
            var b = NewBattle(new[] { "slime_verde" });
            b.EndPlayerTurn();
            Assert.AreEqual(0, b.Deck.Hand.Count);
            Assert.AreEqual(5, b.Deck.DiscardPile.Count);
            b.RunEnemyTurn();
            Assert.AreEqual(BattlePhase.PlayerTurn, b.Phase);
            Assert.AreEqual(3, b.Energy);
            Assert.AreEqual(5, b.Deck.Hand.Count);
        }

        [Test]
        public void BaralhoVazioEmbaralhaODescarteEContinuaComprando()
        {
            var b = NewBattle(new[] { "slime_verde" });
            int total = b.Deck.TotalCards;
            b.EndPlayerTurn(); b.RunEnemyTurn();   // turno 2: compra as outras 5
            Assert.AreEqual(0, b.Deck.DrawPile.Count);
            b.EndPlayerTurn(); b.RunEnemyTurn();   // turno 3: baralho vazio, reembaralha
            Assert.AreEqual(5, b.Deck.Hand.Count);
            Assert.AreEqual(total, b.Deck.TotalCards, "nenhuma carta se perde");
        }

        [Test]
        public void ComprarComTudoVazioNaoQuebra()
        {
            var d = new DeckManager(new List<CardInstance> { Card("golpe"), Card("golpe") }, new SeededRng(1));
            Assert.AreEqual(2, d.Draw(5));
            Assert.AreEqual(0, d.Draw(5));
        }

        [Test]
        public void EsgotarTiraACartaDaBatalha()
        {
            var b = NewBattle(new[] { "slime_verde" }, heroHp: 30);
            Hand(b, Card("seiva"));
            Play(b, 0);
            Assert.AreEqual(1, b.Deck.ExhaustPile.Count);
            Assert.AreEqual(0, b.Deck.DiscardPile.Count);
        }

        // ---------- Cartas ----------

        [Test]
        public void GolpeCausa6()
        {
            var b = NewBattle(new[] { "slime_azul" });
            Hand(b, Card("golpe"));
            Play(b, 0);
            Assert.AreEqual(22 - 6, b.Enemies[0].Hp);
        }

        [Test]
        public void GolpeMelhoradoCausa9()
        {
            var b = NewBattle(new[] { "slime_azul" });
            Hand(b, Card("golpe", up: true));
            Play(b, 0);
            Assert.AreEqual(22 - 9, b.Enemies[0].Hp);
        }

        [Test]
        public void DefenderDa5DeEscudo()
        {
            var b = NewBattle(new[] { "slime_verde" });
            Hand(b, Card("defender"));
            Play(b, 0, -1);
            Assert.AreEqual(5, b.Hero.Shield);
        }

        [Test]
        public void BolaDeFogoEficazContraPlantaMostraEvento()
        {
            var b = NewBattle(new[] { "slime_verde" });
            Hand(b, Card("bola_de_fogo"));
            var ev = Play(b, 0);
            Assert.AreEqual(18 - 10, b.Enemies[0].Hp);
            Assert.IsTrue(ev.Any(e => e.kind == BattleEventKind.Damage && e.effective));
        }

        [Test]
        public void BolaDeFogoContraAguaNaoEEficaz()
        {
            var b = NewBattle(new[] { "slime_azul" });
            Hand(b, Card("bola_de_fogo"));
            var ev = Play(b, 0);
            Assert.AreEqual(22 - 7, b.Enemies[0].Hp);
            Assert.IsFalse(ev.Any(e => e.effective));
        }

        [Test]
        public void OndaAtingeTodosOsInimigos()
        {
            var b = NewBattle(new[] { "slime_azul", "slime_vermelho", "slime_verde" });
            Hand(b, Card("onda"));
            Play(b, 0, -1);
            Assert.AreEqual(22 - 5, b.Enemies[0].Hp);
            Assert.AreEqual(14 - 7, b.Enemies[1].Hp, "Água vence Fogo: 5*1,5=7");
            Assert.AreEqual(18 - 5, b.Enemies[2].Hp);
        }

        [Test]
        public void GolpeDuploBateDuasVezesEOEscudoAbsorvePrimeiro()
        {
            var b = NewBattle(new[] { "slime_azul" });
            b.Enemies[0].Shield = 5;
            Hand(b, Card("golpe_duplo"));
            var ev = Play(b, 0);
            Assert.AreEqual(2, ev.Count(e => e.kind == BattleEventKind.Damage));
            Assert.AreEqual(22 - 3, b.Enemies[0].Hp);  // 4 + 4 = 8, 5 absorvidos
        }

        [Test]
        public void RaizCausaDanoEGanhaEscudo()
        {
            var b = NewBattle(new[] { "slime_azul" });
            Hand(b, Card("raiz"));
            Play(b, 0);
            Assert.AreEqual(22 - 6, b.Enemies[0].Hp, "Planta vence Água: 4*1,5=6");
            Assert.AreEqual(4, b.Hero.Shield);
        }

        [Test]
        public void SeivaCuraEEsgota()
        {
            var b = NewBattle(new[] { "slime_verde" }, heroHp: 30);
            Hand(b, Card("seiva"));
            Play(b, 0, -1);
            Assert.AreEqual(36, b.Hero.Hp);
        }

        [Test]
        public void RespingoAplicaFraco()
        {
            var b = NewBattle(new[] { "slime_vermelho" });
            Hand(b, Card("respingo"));
            Play(b, 0);
            Assert.AreEqual(1, b.Enemies[0].Weak);
            Assert.AreEqual(14 - 6, b.Enemies[0].Hp, "Água vence Fogo: 4*1,5=6");
        }

        // ---------- Status ----------

        [Test]
        public void QueimaduraTira2NoInicioDoTurnoDoAlvoEGastaTurnos()
        {
            var b = NewBattle(new[] { "slime_verde" });
            Hand(b, Card("brasa"));
            Play(b, 0);
            Assert.AreEqual(3, b.Enemies[0].Burn);
            b.EndPlayerTurn();
            b.RunEnemyTurn();
            Assert.AreEqual(18 - 2, b.Enemies[0].Hp);
            Assert.AreEqual(2, b.Enemies[0].Burn);
        }

        [Test]
        public void ReaplicarQueimaduraSomaTurnos()
        {
            var b = NewBattle(new[] { "slime_azul" });
            Hand(b, Card("brasa"), Card("brasa", up: true));
            Play(b, 0); Play(b, 0);
            Assert.AreEqual(8, b.Enemies[0].Burn);
        }

        [Test]
        public void QueimaduraIgnoraOEscudo()
        {
            var c = new Combatant { Hp = 20, MaxHp = 20, Shield = 10 };
            Assert.AreEqual(2, c.LoseHp(2));
            Assert.AreEqual(18, c.Hp);
            Assert.AreEqual(10, c.Shield);
        }

        [Test]
        public void SlimeVermelhoAtaca7ENoTerceiroTurnoQueima()
        {
            var b = NewBattle(new[] { "slime_vermelho" });
            var verm = b.Enemies[0];
            b.EndPlayerTurn(); b.RunEnemyTurn();      // turno 1: ataque 7
            b.EndPlayerTurn(); b.RunEnemyTurn();      // turno 2: ataque 7
            Assert.AreEqual(50 - 14, b.Hero.Hp);
            Assert.AreEqual("Chama", verm.Intent.name, "a cada 3 turnos, em vez de atacar, queima");
            b.EndPlayerTurn(); b.RunEnemyTurn();      // turno 3: Queimadura 2; no início do turno do herói tira 2
            Assert.AreEqual(50 - 14 - 2, b.Hero.Hp, "a queimadura não é ataque: só o tique de 2");
            Assert.AreEqual(1, b.Hero.Burn);
            Assert.AreEqual("Investida", verm.Intent.name);
        }

        static string EnemyIntentName(Enemy e) => e.Intent.name;

        [Test]
        public void QueimaduraDoHeroiCausa2NoInicioDoTurno()
        {
            var b = NewBattle(new[] { "slime_vermelho" });
            b.Hero.Burn = 2;
            b.EndPlayerTurn(); b.RunEnemyTurn();   // ataque 7 e novo turno: queimadura tira 2
            Assert.AreEqual(50 - 7 - 2, b.Hero.Hp);
            Assert.AreEqual(1, b.Hero.Burn);
        }

        [Test]
        public void FracoReduzODanoDoInimigoEGastaTurno()
        {
            var b = NewBattle(new[] { "slime_vermelho" });
            b.Enemies[0].Weak = 1;
            b.EndPlayerTurn(); b.RunEnemyTurn();
            Assert.AreEqual(50 - 5, b.Hero.Hp, "7 * 0,75 = 5,25 -> 5");
            Assert.AreEqual(0, b.Enemies[0].Weak);
        }

        [Test]
        public void ForcaDoInimigoSomaNoDano()
        {
            var b = NewBattle(new[] { "slime_vermelho" });
            b.Enemies[0].Strength = 3;
            Assert.AreEqual(10, b.PreviewDamage(b.Enemies[0]));
        }

        [Test]
        public void AtordoadoPulaAAcaoEMantemAIntencao()
        {
            var b = NewBattle(new[] { "slime_vermelho" });
            Hand(b, Card("investida"));
            Play(b, 0);
            Assert.IsTrue(b.Enemies[0].Stunned);
            var intent = b.Enemies[0].Intent;
            b.EndPlayerTurn(); b.RunEnemyTurn();
            Assert.AreEqual(50, b.Hero.Hp, "não atacou");
            Assert.IsFalse(b.Enemies[0].Stunned);
            Assert.AreSame(intent, b.Enemies[0].Intent);
            b.EndPlayerTurn(); b.RunEnemyTurn();
            Assert.AreEqual(50 - 7, b.Hero.Hp, "agora ataca");
        }

        [Test]
        public void EscudoDoHeroiProtegeNoTurnoDosInimigosEZeraNoInicioDoSeuTurno()
        {
            var b = NewBattle(new[] { "slime_azul" });
            b.Hero.Shield = 10;
            b.EndPlayerTurn();
            b.RunEnemyTurn();
            Assert.AreEqual(50, b.Hero.Hp, "o escudo absorveu o ataque de 4");
            Assert.AreEqual(0, b.Hero.Shield, "zerou ao começar o turno do herói");
        }

        [Test]
        public void EscudoDoInimigoZeraNoInicioDoTurnoDele()
        {
            var b = NewBattle(new[] { "slime_azul" });
            b.Enemies[0].Shield = 9;
            b.EndPlayerTurn();
            var play = b.NextEnemyAction();
            Assert.AreEqual(0, b.Enemies[0].Shield);
            play.Resolve();
        }

        // ---------- IA ----------

        [Test]
        public void SlimeVerdeNuncaFazEscudoDuasVezesSeguidas()
        {
            for (int seed = 0; seed < 40; seed++)
            {
                var b = NewBattle(new[] { "slime_verde" }, seed, heroHp: 50);
                int last = -1;
                for (int turn = 0; turn < 12 && !b.IsOver; turn++)
                {
                    int idx = b.Enemies[0].IntentIndex;
                    Assert.IsFalse(idx == 1 && last == 1, $"seed {seed}, turno {turn}");
                    last = idx;
                    b.Hero.Hp = 50;
                    b.EndPlayerTurn(); b.RunEnemyTurn();
                }
            }
        }

        [Test]
        public void SlimeVerdeAtaca5OuGanha4DeEscudo()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var b = NewBattle(new[] { "slime_verde" }, seed);
                bool defend = b.Enemies[0].Intent.intent == IntentType.Defend;
                b.EndPlayerTurn(); b.RunEnemyTurn();
                if (defend) { Assert.AreEqual(50, b.Hero.Hp); Assert.AreEqual(4, b.Enemies[0].Shield); }
                else Assert.AreEqual(45, b.Hero.Hp);
            }
        }

        [Test]
        public void SlimeAzulAtaca4ECuraOAliadoMaisFerido()
        {
            var b = NewBattle(new[] { "slime_azul", "slime_verde" });
            b.Enemies[1].Hp = 10; // o verde está mais ferido (8 faltando)
            b.Enemies[0].Hp = 20; // o azul falta 2
            b.EndPlayerTurn();
            var play = b.NextEnemyAction();           // o azul age primeiro
            Assert.AreEqual("Slime Azul", play.Enemy.Name);
            play.Resolve();
            Assert.AreEqual(46, b.Hero.Hp);
            Assert.AreEqual(13, b.Enemies[1].Hp);
            Assert.AreEqual(20, b.Enemies[0].Hp);
        }

        [Test]
        public void SlimeAzulSozinhoCuraASiMesmo()
        {
            var b = NewBattle(new[] { "slime_azul" });
            b.Enemies[0].Hp = 10;
            b.EndPlayerTurn(); b.RunEnemyTurn();
            Assert.AreEqual(13, b.Enemies[0].Hp);
        }

        [Test]
        public void InimigosAgemDaEsquerdaParaADireita()
        {
            var b = NewBattle(new[] { "slime_vermelho", "slime_azul" });
            b.EndPlayerTurn();
            var order = new List<string>();
            EnemyBattlePlay(b, order);
            CollectionAssert.AreEqual(new[] { "Slime Vermelho", "Slime Azul" }, order);
        }

        static void EnemyBattlePlay(BattleSystem b, List<string> order)
        {
            BattleSystem.EnemyPlay p;
            while ((p = b.NextEnemyAction()) != null) { order.Add(p.Enemy.Name); p.Resolve(); }
        }

        // ---------- Chefe ----------

        [Test]
        public void ReiSlimeSegueOCicloEsmagarDividirCoroaPesada()
        {
            var b = NewBattle(new[] { "rei_slime" });
            var king = b.Enemies[0];
            Assert.AreEqual("Esmagar", king.Intent.name);
            b.EndPlayerTurn(); b.RunEnemyTurn();
            Assert.AreEqual(50 - 12, b.Hero.Hp);
            Assert.AreEqual("Dividir", king.Intent.name);
            b.EndPlayerTurn(); b.RunEnemyTurn();
            Assert.AreEqual(3, b.Enemies.Count, "invocou 2 Slimes Mirins");
            Assert.AreEqual("Coroa Pesada", king.Intent.name);
            b.Hero.Hp = 50;
            b.EndPlayerTurn(); b.RunEnemyTurn();
            Assert.AreEqual(8, king.Shield);
        }

        [Test]
        public void ReiSlimeNaoInvocaAlemDoMaximoDe2Vivos()
        {
            var b = NewBattle(new[] { "rei_slime" });
            for (int i = 0; i < 9 && !b.IsOver; i++)   // 3 voltas do ciclo
            {
                b.Hero.Hp = 50; b.Hero.Shield = 0;
                b.EndPlayerTurn(); b.RunEnemyTurn();
            }
            Assert.AreEqual(2, b.Enemies.Count(e => e.Data.id == "slime_mirim" && e.IsAlive));
            Assert.AreEqual(2, b.Enemies.Count(e => e.Data.id == "slime_mirim"), "nunca passou de 2 no total");
        }

        [Test]
        public void ReiSlimeInvocaDeNovoSeOsMirinsMorreram()
        {
            var b = NewBattle(new[] { "rei_slime" });
            b.EndPlayerTurn(); b.RunEnemyTurn();                 // Esmagar
            b.EndPlayerTurn(); b.RunEnemyTurn();                 // Dividir
            foreach (var e in b.Enemies.Where(e => e.Data.id == "slime_mirim")) e.Hp = 0;
            b.Hero.Hp = 50;
            b.EndPlayerTurn(); b.RunEnemyTurn();                 // Coroa Pesada
            b.Hero.Hp = 50;
            b.EndPlayerTurn(); b.RunEnemyTurn();                 // Esmagar
            b.Hero.Hp = 50;
            b.EndPlayerTurn(); b.RunEnemyTurn();                 // Dividir de novo
            Assert.AreEqual(2, b.Enemies.Count(e => e.Data.id == "slime_mirim" && e.IsAlive));
        }

        [Test]
        public void ReiSlimeGanha2DeForcaAbaixoDe50PorCentoApenasUmaVez()
        {
            var b = NewBattle(new[] { "rei_slime" });
            var king = b.Enemies[0];
            king.Hp = 36;                              // 70 * 0,5 = 35
            Hand(b, Card("golpe"), Card("golpe"));
            var ev = Play(b, 0);                       // 30 de vida: abaixo de 50%
            Assert.AreEqual(2, king.Strength);
            Assert.IsTrue(ev.Any(e => e.kind == BattleEventKind.Enrage));
            ev = Play(b, 0);
            Assert.AreEqual(2, king.Strength, "só uma vez");
            Assert.IsFalse(ev.Any(e => e.kind == BattleEventKind.Enrage));
        }

        [Test]
        public void ReiSlimeComExatamente50PorCentoNaoEnfurece()
        {
            var b = NewBattle(new[] { "rei_slime" });
            b.Enemies[0].Hp = 41;
            Hand(b, Card("golpe"));
            Play(b, 0);                                // 35 = exatamente 50%
            Assert.AreEqual(0, b.Enemies[0].Strength);
        }

        [Test]
        public void SlimeMirimAtaca3()
        {
            var b = NewBattle(new[] { "slime_mirim" });
            b.EndPlayerTurn(); b.RunEnemyTurn();
            Assert.AreEqual(47, b.Hero.Hp);
        }

        // ---------- Fim da batalha ----------

        [Test]
        public void VitoriaQuandoTodosMorrem()
        {
            var b = NewBattle(new[] { "slime_mirim" });
            Hand(b, Card("golpe_pesado"));
            var ev = Play(b, 0);
            Assert.AreEqual(BattlePhase.Victory, b.Phase);
            Assert.IsTrue(ev.Any(e => e.kind == BattleEventKind.Death));
            Assert.IsFalse(b.CanPlay(0));
        }

        [Test]
        public void DerrotaQuandoOHeroiMorre()
        {
            var b = NewBattle(new[] { "slime_vermelho" }, heroHp: 5);
            b.EndPlayerTurn(); b.RunEnemyTurn();
            Assert.AreEqual(BattlePhase.Defeat, b.Phase);
            Assert.IsFalse(b.Hero.IsAlive);
        }

        [Test]
        public void QueimaduraPodeMatarOHeroiNoInicioDoTurno()
        {
            var b = NewBattle(new[] { "slime_azul" }, heroHp: 7);
            b.Hero.Burn = 3;
            b.EndPlayerTurn();
            b.RunEnemyTurn();                          // azul ataca 4 (sobra 3), queimadura tira 2
            Assert.AreEqual(1, b.Hero.Hp);
            b.Hero.Burn = 3;
            b.EndPlayerTurn(); b.RunEnemyTurn();       // ataca 4, derrota
            Assert.AreEqual(BattlePhase.Defeat, b.Phase);
        }

        [Test]
        public void QueimaduraPodeMatarUmInimigoNoTurnoDele()
        {
            var b = NewBattle(new[] { "slime_mirim" });
            b.Enemies[0].Hp = 2; b.Enemies[0].Burn = 2;
            b.EndPlayerTurn();
            var all = b.RunEnemyTurn();
            Assert.AreEqual(BattlePhase.Victory, b.Phase);
            Assert.IsTrue(all.Any(e => e.kind == BattleEventKind.Death));
        }

        [Test]
        public void MesmaSementeDaMesmaBatalha()
        {
            string Run(int seed)
            {
                var b = NewBattle(new[] { "slime_verde", "slime_azul", "slime_vermelho" }, seed);
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < 6 && !b.IsOver; i++)
                {
                    sb.Append(string.Join(",", b.Deck.Hand.Select(c => c.Data.id))).Append('|');
                    b.Hero.Hp = 50;
                    b.EndPlayerTurn(); b.RunEnemyTurn();
                }
                return sb.ToString();
            }
            Assert.AreEqual(Run(5), Run(5));
        }
    }
}
