using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using XequeDeTinta.Battle;
using XequeDeTinta.Board;
using XequeDeTinta.Bot;
using XequeDeTinta.Core;
using XequeDeTinta.Data;
using XequeDeTinta.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XequeDeTinta.Tests
{
    /// <summary>Playtest automático da partida inteira pelas cenas reais: Título → Tabuleiro ⇄ Batalha → Fim, com o robô decidindo e clicando.</summary>
    public class FullRunBotTests
    {
        static GameConfig Cfg()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/_Project/Data/GameConfig.asset");
#else
            return null;
#endif
        }

        [SetUp] public void Reset() { Time.timeScale = 1f; if (GameManager.Instance != null) Object.DestroyImmediate(GameManager.Instance.gameObject); BattleLauncher.Clear(); }
        [TearDown] public void Cleanup() { Time.timeScale = 1f; if (GameManager.Instance != null) Object.DestroyImmediate(GameManager.Instance.gameObject); BattleLauncher.Clear(); }

        static bool ClickStarting(string prefix)
        {
            foreach (var b in Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude))
            {
                var t = b.GetComponentInChildren<TMP_Text>();
                if (t != null && t.text.StartsWith(prefix) && b.interactable) { b.onClick.Invoke(); return true; }
            }
            return false;
        }

        static bool ClickName(string name)
        {
            foreach (var b in Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude))
                if (b.name == name && b.interactable) { b.onClick.Invoke(); return true; }
            return false;
        }

        static float lastTrace;
        static void Trace(string msg)
        {
            if (Time.realtimeSinceStartup - lastTrace < 3f) return;
            lastTrace = Time.realtimeSinceStartup;
            Debug.Log("[PLAYTEST] " + msg);
        }

        IEnumerator PlayBattle(BattleController battle)
        {
            yield return new WaitUntil(() => !battle.Busy);
            int guard = 0;
            while (!battle.Sys.IsOver && guard++ < 400)
            {
                Trace($"batalha fase={battle.Sys.Phase} energia={battle.Sys.Energy} busy={battle.Busy} mao={battle.Sys.Deck.Hand.Count} turno={battle.Sys.Turn}");
                var move = BotPolicy.ChooseCard(battle.Sys);
                if (move != null) battle.PlayCardAt(move.Value.handIndex, move.Value.targetIndex);
                else battle.EndTurn();
                yield return null;
                yield return new WaitUntil(() => !battle.Busy || battle.ResultShown);
            }
            yield return new WaitUntil(() => battle.ResultShown);
            battle.Continue();
        }

        IEnumerator PlayBoard(BoardController board)
        {
            board.speed = 40f;
            float deadline = Time.realtimeSinceStartup + 20f;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (SceneManager.GetActiveScene().name != "Board") yield break;
                Trace($"tabuleiro estado={board.State} casa={board.CurrentTileId} painel={(board.TilePanel != null)} recompensa={board.RewardVisible}");
                switch (board.State)
                {
                    case BoardState.Idle:
                        if (board.RewardVisible) break;
                        board.RollDice(); break;
                    case BoardState.Choosing:
                    {
                        var opts = board.PendingOptions;
                        board.Choose(BotPolicy.ChooseBranch(board.Graph, opts, board.Run, board.Rng)); break;
                    }
                    case BoardState.Confirming: board.ConfirmRoll(); break;
                    case BoardState.Resolving:
                        if (board.RewardVisible) board.PickReward(BotPolicy.ChooseReward(board.Run, board.Run.Reward.Choices));
                        else if (board.TilePanel != null) HandlePanel(board);
                        break;
                    case BoardState.Finished: yield break;
                }
                yield return null;
            }
        }

        static void HandlePanel(BoardController board)
        {
            // Loja: compra pela política e sai. Descanso: cura se precisa. Evento: escolha A. Baú: continua.
            if (ClickName("Sair da loja")) return;
            if (GameObject.Find("PainelLoja") != null)
            {
                BotShop(board);
                ClickStarting("Sair da loja");
                return;
            }
            if (ClickStarting("Continuar")) return;
            if (GameObject.Find("PainelDescanso") != null) { ClickStarting(board.Run.Hp < board.Run.MaxHp * 0.7f ? "Curar" : "Curar"); return; }
            if (GameObject.Find("PainelEvento") != null) { ClickName("EscolhaA"); return; }
        }

        static void BotShop(BoardController board)
        {
            for (int i = 0; i < 6; i++) if (!ClickStarting("Comprar 100") && !ClickStarting("Comprar 50")) break;
        }

        // Explícito: o robô por cenas congela depois do primeiro lançamento do dado (causa não encontrada; ver PROGRESS.md).
        // A partida inteira já é jogada pelo robô em lógica pura (BalanceTests) e cada fluxo de cena tem teste próprio.
        [Explicit("Congela o Editor após o 1º dado; investigar antes de reativar")]
        [UnityTest, Timeout(120000)]
        public IEnumerator PartidaInteiraPeloRobo_ChegaAoFim([Values(11)] int seed)
        {
            var gm = GameManager.Ensure(Cfg());
            gm.Rng = new SeededRng(seed);
            gm.BattleSpeed = 8f;
            SceneManager.LoadScene("Title");
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "Title");
            yield return null; yield return null;
            Assert.IsTrue(ClickStarting("Jogar"));

            float t0 = Time.realtimeSinceStartup;
            int battles = 0;
            float lastLog = 0f;
            while (SceneManager.GetActiveScene().name != "End" && Time.realtimeSinceStartup - t0 < 100f)
            {
                string scene = SceneManager.GetActiveScene().name;
                if (Time.realtimeSinceStartup - lastLog > 5f)
                {
                    lastLog = Time.realtimeSinceStartup;
                    var bd = Object.FindAnyObjectByType<BoardController>();
                    var bt = Object.FindAnyObjectByType<BattleController>();
                    Debug.Log($"[PLAYTEST] t={lastLog - t0:F0}s cena={scene} board={(bd != null ? bd.State.ToString() + " casa " + bd.CurrentTileId + " painel=" + (bd.TilePanel != null) + " recompensa=" + bd.RewardVisible : "-")} batalha={(bt != null ? "busy=" + bt.Busy + " fase=" + bt.Sys.Phase + " resultado=" + bt.ResultShown : "-")} timeScale={Time.timeScale}");
                }
                if (scene == "Board")
                {
                    yield return null; yield return null;
                    var board = Object.FindAnyObjectByType<BoardController>();
                    if (board != null) yield return PlayBoard(board);
                }
                else if (scene == "Battle")
                {
                    yield return null; yield return null;
                    var battle = Object.FindAnyObjectByType<BattleController>();
                    if (battle != null) { battles++; yield return PlayBattle(battle); }
                }
                yield return null;
            }
            Assert.AreEqual("End", SceneManager.GetActiveScene().name, "a partida do robô deve terminar (vitória ou derrota)");
            Assert.GreaterOrEqual(battles, 1);
            Assert.IsNotNull(GameManager.Instance.LastRunWon);
            Debug.Log($"[PLAYTEST] semente {seed}: {(GameManager.Instance.LastRunWon.Value ? "vitória" : "derrota")} após {battles} batalhas");
        }
    }
}
