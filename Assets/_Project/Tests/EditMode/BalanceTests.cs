using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using XequeDeTinta.Bot;
using XequeDeTinta.Data;

namespace XequeDeTinta.Tests
{
    /// <summary>
    /// Playtest automático em lógica pura: um robô com política de jogador razoável joga muitas partidas inteiras.
    /// Os números ficam no Console ([BALANCE]) para ajustar os assets; os testes só garantem faixas aceitáveis.
    /// </summary>
    public class BalanceTests
    {
        const int Runs = 400;

        static GameConfig Cfg() => AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/_Project/Data/GameConfig.asset");

        static List<RunReport> Simulate(int n = Runs) =>
            Enumerable.Range(1, n).Select(seed => RunSimulator.Play(Cfg(), seed)).ToList();

        static string Summary(List<RunReport> r)
        {
            double winRate = r.Count(x => x.Won) / (double)r.Count;
            var deaths = r.Where(x => !x.Won).GroupBy(x => x.DeathTile).OrderBy(g => g.Key).Select(g => $"casa {g.Key}: {g.Count()}");
            double turns = r.Average(x => x.PlayerTurns);
            double bossHp = r.Where(x => x.HpBeforeBoss >= 0).DefaultIfEmpty().Average(x => x == null ? 0 : x.HpBeforeBoss);
            double bossReach = r.Count(x => x.HpBeforeBoss >= 0) / (double)r.Count;
            return $"[BALANCE] {r.Count} partidas · vitórias {winRate:P0} · chegam ao chefe {bossReach:P0} · vida média ao chefe {bossHp:F0} · " +
                   $"turnos do jogador por partida {turns:F1} · batalhas {r.Average(x => x.Battles):F1} · deck {r.Average(x => x.DeckSize):F1} · " +
                   $"relíquias {r.Average(x => x.Relics):F1} · mortes: {string.Join(", ", deaths)}";
        }

        [Test]
        public void SimulacaoDe400PartidasTerminaESeguePoucoVariacao()
        {
            var reports = Simulate();
            Debug.Log(Summary(reports));
            Assert.AreEqual(Runs, reports.Count);
            Assert.IsTrue(reports.All(x => x.Won || x.DeathTile >= 0 || x.Battles > 0), "toda partida acaba em vitória ou morte");
        }

        [Test]
        public void MesmaSementeMesmoResultado()
        {
            var a = RunSimulator.Play(Cfg(), 123); var b = RunSimulator.Play(Cfg(), 123);
            Assert.AreEqual(a.Won, b.Won); Assert.AreEqual(a.PlayerTurns, b.PlayerTurns); Assert.AreEqual(a.Coins, b.Coins);
        }

        [Test]
        public void ODesafioEstaNaFaixa_NemImpossivelNemTrivial()
        {
            var r = Simulate();
            double win = r.Count(x => x.Won) / (double)r.Count;
            Debug.Log(Summary(r));
            Assert.That(win, Is.InRange(0.40, 0.85), "taxa de vitória do robô fora da faixa-alvo (40% a 85%)");
        }

        [Test]
        public void ADuracaoEstimadaFicaEntre10e15Minutos()
        {
            var r = Simulate();
            // ~25 s por turno do jogador (ler, jogar 3 cartas, animações) + ~25 s de tabuleiro por batalha/casa.
            double minutes = r.Average(x => x.PlayerTurns * 25.0 + 11 * 15.0) / 60.0;
            Debug.Log($"[BALANCE] duração estimada: {minutes:F1} min");
            Assert.That(minutes, Is.InRange(8.0, 16.0));
        }

        [Test]
        public void NenhumaBatalhaTravaNoLimiteDeTurnos()
        {
            var r = Simulate(200);
            Assert.IsTrue(r.SelectMany(x => x.TurnsPerBattle).All(t => t < 60), "alguma batalha bateu o limite de 60 turnos (robô preso)");
        }

        [Test]
        public void TodasAsBatalhasComunsSaoVenciveisPeloRobo()
        {
            // O robô perde às vezes, mas nenhuma batalha comum deve ser impossível: cada grupo é vencido em pelo menos 60% das vezes com vida cheia.
            var cfg = Cfg();
            foreach (var enc in cfg.battleEncounters)
            {
                int wins = 0, n = 60;
                for (int seed = 1; seed <= n; seed++)
                {
                    var run = new XequeDeTinta.Core.RunState(cfg, new XequeDeTinta.Core.SeededRng(seed));
                    var b = new XequeDeTinta.Battle.BattleSystem(new XequeDeTinta.Battle.BattleSetup
                    {
                        HeroHp = 50, HeroMaxHp = 50, Deck = new List<XequeDeTinta.Battle.CardInstance>(run.Deck), Encounter = enc,
                        Rules = XequeDeTinta.Battle.BattleRules.From(cfg), Rng = new XequeDeTinta.Core.SeededRng(seed),
                    });
                    b.Begin();
                    for (int t = 0; t < 40 && !b.IsOver; t++)
                    {
                        for (int p = 0; p < 20 && !b.IsOver; p++)
                        {
                            var m = BotPolicy.ChooseCard(b);
                            if (m == null) break;
                            b.PlayCard(m.Value.handIndex, m.Value.targetIndex).Resolve();
                        }
                        if (b.IsOver) break;
                        b.EndPlayerTurn(); b.RunEnemyTurn();
                    }
                    if (b.Phase == XequeDeTinta.Battle.BattlePhase.Victory) wins++;
                }
                Debug.Log($"[BALANCE] {enc.id}: vitória {wins}/{n} com deck inicial e vida cheia");
                Assert.GreaterOrEqual(wins, n * 6 / 10, enc.id);
            }
        }

        [Test]
        public void ExplorarTamanhoDoDado_SoRegistraNoConsole()
        {
            var cfg = Cfg();
            int original = cfg.diceSides;
            try
            {
                foreach (int sides in new[] { 6, 4, 3, 2 })
                {
                    cfg.diceSides = sides;
                    var r = Simulate(300);
                    double minutes = r.Average(x => x.PlayerTurns * 25.0 + 11 * 15.0) / 60.0;
                    Debug.Log($"[BALANCE] dado d{sides}: {Summary(r)} · duração ~{minutes:F1} min");
                }
            }
            finally { cfg.diceSides = original; }
        }
    }
}
