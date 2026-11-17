using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using XequeDeTinta.Battle;
using XequeDeTinta.Board;
using XequeDeTinta.Core;
using XequeDeTinta.Data;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XequeDeTinta.Tests
{
    /// <summary>Fluxos da partida: tabuleiro -> batalha -> recompensa -> tabuleiro, e derrota -> título.</summary>
    public class RunFlowTests
    {
        static GameConfig Cfg()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/_Project/Data/GameConfig.asset");
#else
            return null;
#endif
        }

        [SetUp]
        public void Reset()
        {
            if (GameManager.Instance != null) Object.DestroyImmediate(GameManager.Instance.gameObject);
            BattleLauncher.Clear();
        }

        [TearDown]
        public void Cleanup()
        {
            if (GameManager.Instance != null) Object.DestroyImmediate(GameManager.Instance.gameObject);
            BattleLauncher.Clear();
        }

        IEnumerator WaitFor(System.Func<bool> cond, float timeout = 20f)
        {
            float t = 0;
            while (!cond() && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(cond(), "tempo esgotado esperando a condição");
        }

        IEnumerator WaitScene(string name) { yield return WaitFor(() => SceneManager.GetActiveScene().name == name); yield return null; yield return null; }

        /// <summary>Joga Golpes (1 de vida por inimigo) até vencer; ao vencer o controlador fica ocupado até o painel de resultado.</summary>
        IEnumerator WinBattle(BattleController battle)
        {
            foreach (var e in battle.Sys.Enemies) e.Hp = 1;
            while (!battle.Sys.IsOver)
            {
                battle.PlayCardAt(0, battle.Sys.Enemies.FindIndex(e => e.IsAlive));
                yield return WaitFor(() => !battle.Busy || battle.ResultShown);
            }
            yield return WaitFor(() => battle.ResultShown);
        }

        /// <summary>Começa a partida pelo tabuleiro com um deck só de Golpes (batalhas previsíveis).</summary>
        IEnumerator NewRunOnBoard()
        {
            var gm = GameManager.Ensure(Cfg());
            gm.BattleSpeed = 6f;
            gm.NewRun();
            var golpe = Cfg().starterDeck.First(c => c.id == "golpe");
            gm.Run.Deck.Clear();
            for (int i = 0; i < 10; i++) gm.Run.AddCard(golpe);
            SceneManager.LoadScene("Board");
            yield return WaitScene("Board");
        }

        [UnityTest]
        public IEnumerator BatalhaNoTabuleiro_Vence_RecebeRecompensa_E_Volta()
        {
            yield return NewRunOnBoard();
            var board = Object.FindAnyObjectByType<BoardController>();
            board.speed = 40f;
            var run = GameManager.Instance.Run;

            // Dado 1: o peão para na casa 1 (Batalha), que entra na cena Battle.
            board.Rng = new ScriptedRng(1);
            board.RollDice();
            yield return WaitScene("Battle");

            var battle = Object.FindAnyObjectByType<BattleController>();
            yield return WaitFor(() => !battle.Busy);
            yield return WinBattle(battle);
            Assert.IsTrue(battle.Result.Won);
            battle.Continue();

            // Volta ao tabuleiro com a recompensa na tela.
            yield return WaitScene("Board");
            board = Object.FindAnyObjectByType<BoardController>();
            yield return WaitFor(() => board.RewardVisible);
            Assert.AreEqual(BoardState.Resolving, board.State);
            Assert.IsFalse(board.Run.Progress.IsVisited(1), "ainda não resolvida: falta escolher a recompensa");
            var pool = Cfg().rewardPool;
            var offered = board.Run.Reward.Choices;
            Assert.AreEqual(3, offered.Count);
            Assert.IsTrue(offered.All(c => pool.Contains(c)));
            int coinsOffered = board.Run.Reward.Coins;
            Assert.That(coinsOffered, Is.InRange(10, 15));

            board.PickReward(1);
            yield return WaitFor(() => board.State == BoardState.Idle);
            Assert.IsFalse(board.RewardVisible);
            Assert.AreEqual(coinsOffered, board.Run.Coins);
            Assert.AreEqual(11, board.Run.Deck.Count);
            Assert.AreEqual(offered[1], board.Run.Deck.Last().Data);
            Assert.IsTrue(board.Run.Progress.IsVisited(1), "a casa fica cinza");
            Assert.AreEqual(1, board.CurrentTileId);
            Assert.AreEqual(-1, board.Run.PendingTileId);
        }

        [UnityTest]
        public IEnumerator PularARecompensaDaAsMoedasSemCarta()
        {
            yield return NewRunOnBoard();
            var board = Object.FindAnyObjectByType<BoardController>();
            board.speed = 40f;
            board.Rng = new ScriptedRng(1);
            board.RollDice();
            yield return WaitScene("Battle");
            var battle = Object.FindAnyObjectByType<BattleController>();
            yield return WaitFor(() => !battle.Busy);
            yield return WinBattle(battle);
            battle.Continue();
            yield return WaitScene("Board");
            board = Object.FindAnyObjectByType<BoardController>();
            yield return WaitFor(() => board.RewardVisible);
            board.PickReward(-1);
            yield return WaitFor(() => board.State == BoardState.Idle);
            Assert.AreEqual(10, board.Run.Deck.Count);
            Assert.That(board.Run.Coins, Is.InRange(10, 15));
        }

        [UnityTest]
        public IEnumerator VidaPersisteEntreBatalhaETabuleiro()
        {
            yield return NewRunOnBoard();
            var board = Object.FindAnyObjectByType<BoardController>();
            board.speed = 40f;
            board.Rng = new ScriptedRng(1);
            board.RollDice();
            yield return WaitScene("Battle");
            var battle = Object.FindAnyObjectByType<BattleController>();
            yield return WaitFor(() => !battle.Busy);
            battle.Sys.Hero.Hp = 23;                                 // sai da batalha com 23 de vida
            yield return WinBattle(battle);
            battle.Continue();
            yield return WaitScene("Board");
            Assert.AreEqual(23, GameManager.Instance.Run.Hp);
        }

        [UnityTest]
        public IEnumerator DerrotaVoltaAoTitulo()
        {
            yield return NewRunOnBoard();
            var board = Object.FindAnyObjectByType<BoardController>();
            board.speed = 40f;
            GameManager.Instance.Run.Hp = 1;
            board.Rng = new ScriptedRng(1);
            board.RollDice();
            yield return WaitScene("Battle");
            var battle = Object.FindAnyObjectByType<BattleController>();
            yield return WaitFor(() => !battle.Busy);
            battle.EndTurn();                                        // com 1 de vida, qualquer ataque derrota
            yield return WaitFor(() => battle.ResultShown, 30f);
            Assert.IsFalse(battle.Result.Won);
            battle.Continue();
            yield return WaitScene("Title");
            Assert.IsNull(GameManager.Instance.Run, "a partida acabou");
            Assert.AreEqual(false, GameManager.Instance.LastRunWon);
        }

        [UnityTest]
        public IEnumerator CasaVisitadaFicaCinzaAoVoltar()
        {
            yield return NewRunOnBoard();
            var run = GameManager.Instance.Run;
            run.Progress.MarkVisited(1);
            run.Progress.CurrentTile = 1;
            SceneManager.LoadScene("Board");
            yield return WaitScene("Board");
            var board = Object.FindAnyObjectByType<BoardController>();
            Assert.AreEqual(1, board.CurrentTileId);
            Assert.IsTrue(board.IsTileVisited(1));
        }
    }
}
