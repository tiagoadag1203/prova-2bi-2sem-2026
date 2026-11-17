using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using XequeDeTinta.Battle;
using XequeDeTinta.Core;
using XequeDeTinta.Data;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XequeDeTinta.Tests
{
    public class BattleFlowTests
    {
        BattleController battle;

        static T Load<T>(string path) where T : Object
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<T>(path);
#else
            return null;
#endif
        }

        static GameConfig Cfg() => Load<GameConfig>("Assets/_Project/Data/GameConfig.asset");

        static EnemyData Enemy(string name) => Load<EnemyData>("Assets/_Project/Data/Enemies/Enemy_" + name + ".asset");

        static CardData Card(string name) => Load<CardData>("Assets/_Project/Data/Cards/Card_" + name + ".asset");

        /// <summary>Prepara a batalha antes de carregar a cena (a cena lê BattleLauncher.Setup no Start).</summary>
        IEnumerator StartBattle(string enemy, int heroHp = 50, string card = "Golpe", float speed = 6f)
        {
            var enc = ScriptableObject.CreateInstance<EncounterData>();
            enc.enemies = new List<EnemyData> { Enemy(enemy) };
            BattleLauncher.Setup = new BattleSetup
            {
                HeroHp = heroHp, HeroMaxHp = 50,
                Deck = Enumerable.Range(0, 10).Select(_ => new CardInstance(Card(card))).ToList(),
                Encounter = enc, Rules = BattleRules.From(Cfg()), Rng = new SeededRng(3),
            };
            BattleLauncher.SpeedOverride = speed;
            SceneManager.LoadScene("Battle");
            yield return null; yield return null;
            battle = Object.FindAnyObjectByType<BattleController>();
            Assert.IsNotNull(battle);
            yield return WaitFor(() => !battle.Busy);
        }

        IEnumerator WaitFor(System.Func<bool> cond, float timeout = 15f)
        {
            float t = 0;
            while (!cond() && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(cond(), "tempo esgotado esperando a condição");
        }

        [TearDown]
        public void Cleanup() => BattleLauncher.Clear();

        [UnityTest]
        public IEnumerator DanoSoEAplicadoNoQuadroDeImpactoDaAnimacao()
        {
            yield return StartBattle("SlimeAzul", speed: 1f);
            var enemy = battle.Sys.Enemies[0];
            Assert.AreEqual(22, enemy.Hp);

            battle.PlayCardAt(0, 0);
            yield return null; yield return null;
            Assert.AreEqual(22, enemy.Hp, "ainda no início da animação de ataque: nada de dano");
            Assert.IsTrue(battle.Busy);

            yield return WaitFor(() => !battle.Busy);
            Assert.AreEqual(16, enemy.Hp, "o Golpe de 6 foi aplicado no impacto");
        }

        [UnityTest]
        public IEnumerator VencerMostraResultadoEEntregaAoGameManager()
        {
            BattleResult delivered = null;
            yield return StartBattle("SlimeMirim");
            BattleLauncher.OnFinished = r => delivered = r;

            battle.PlayCardAt(0, 0);
            yield return WaitFor(() => !battle.Busy);
            battle.PlayCardAt(0, 0);                    // 6 + 6 >= 8 de vida
            yield return WaitFor(() => battle.ResultShown);

            Assert.AreEqual(BattlePhase.Victory, battle.Sys.Phase);
            Assert.IsTrue(battle.Result.Won);
            Assert.AreEqual(1, battle.Result.Kills);
            Assert.AreEqual(50, battle.Result.HeroHp);

            battle.Continue();
            Assert.IsNotNull(delivered);
            Assert.IsTrue(delivered.Won);
        }

        [UnityTest]
        public IEnumerator EncerrarTurnoFazOInimigoAtacarEVoltaParaOJogador()
        {
            yield return StartBattle("SlimeVerde", card: "Defender");
            battle.EndTurn();
            yield return WaitFor(() => battle.Busy);
            yield return WaitFor(() => !battle.Busy);

            Assert.AreEqual(BattlePhase.PlayerTurn, battle.Sys.Phase);
            Assert.AreEqual(5, battle.Sys.Deck.Hand.Count);
            Assert.AreEqual(3, battle.Sys.Energy);
            var e = battle.Sys.Enemies[0];
            bool defended = e.Shield == 4;
            Assert.AreEqual(defended ? 50 : 45, battle.Sys.Hero.Hp);
        }

        [UnityTest]
        public IEnumerator HeroiMorreEMostraDerrota()
        {
            yield return StartBattle("SlimeVermelho", heroHp: 3, card: "Defender");
            battle.EndTurn();
            yield return WaitFor(() => battle.ResultShown);
            Assert.AreEqual(BattlePhase.Defeat, battle.Sys.Phase);
            Assert.IsFalse(battle.Result.Won);
        }

        [UnityTest]
        public IEnumerator CartaSemEnergiaNaoEJogada()
        {
            yield return StartBattle("SlimeAzul", card: "GolpePesado");   // custa 2
            battle.PlayCardAt(0, 0);
            yield return WaitFor(() => !battle.Busy);
            Assert.AreEqual(1, battle.Sys.Energy);
            battle.PlayCardAt(0, 0);                                      // sobra 1: não joga
            yield return null;
            Assert.IsFalse(battle.Busy);
            Assert.AreEqual(1, battle.Sys.Energy);
            Assert.AreEqual(22 - 14, battle.Sys.Enemies[0].Hp);
        }
    }
}
