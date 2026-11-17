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
using XequeDeTinta.Core;
using XequeDeTinta.Data;
using XequeDeTinta.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XequeDeTinta.Tests
{
    /// <summary>Chefe, tela de vitória e derrota, e a pausa.</summary>
    public class EndFlowTests
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

        IEnumerator WaitFor(System.Func<bool> cond, float timeout = 20f)
        {
            float t = 0;
            while (!cond() && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(cond(), "tempo esgotado esperando a condição");
        }

        IEnumerator WaitScene(string name) { yield return WaitFor(() => SceneManager.GetActiveScene().name == name); yield return null; yield return null; }

        static bool Click(string label)
        {
            foreach (var b in Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude))
            {
                var t = b.GetComponentInChildren<TMP_Text>();
                if (t != null && t.text == label && b.interactable) { b.onClick.Invoke(); return true; }
            }
            return false;
        }

        IEnumerator StartAtBoard()
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
        public IEnumerator Chefe_Vence_LevaATelaDeVitoria()
        {
            yield return StartAtBoard();
            var board = Object.FindAnyObjectByType<BoardController>();
            board.speed = 40f;
            board.PlaceAt(10);
            board.Rng = new ScriptedRng(1);
            board.RollDice();                                   // casa 11 = Chefe
            yield return WaitScene("Battle");

            var battle = Object.FindAnyObjectByType<BattleController>();
            yield return WaitFor(() => !battle.Busy);
            Assert.AreEqual("Rei Slime", battle.Sys.Enemies[0].Name);
            Assert.AreEqual(70, battle.Sys.Enemies[0].MaxHp);
            battle.Sys.Enemies[0].Hp = 1;
            battle.PlayCardAt(0, 0);
            yield return WaitFor(() => battle.ResultShown);
            Assert.IsTrue(battle.Result.Won);
            battle.Continue();

            yield return WaitScene("End");
            var end = Object.FindAnyObjectByType<EndController>();
            Assert.IsTrue(end.Won);
            Assert.IsNotNull(GameObject.Find("VoltarTitulo"));
            Assert.IsNull(GameObject.Find("TentarDeNovo"), "vitória não tem 'Tentar de novo'");
        }

        [UnityTest]
        public IEnumerator Derrota_TemTentarDeNovo_ComecaPartidaNova()
        {
            yield return StartAtBoard();
            var board = Object.FindAnyObjectByType<BoardController>();
            board.speed = 40f;
            GameManager.Instance.Run.Hp = 1;
            board.Rng = new ScriptedRng(1);
            board.RollDice();
            yield return WaitScene("Battle");
            var battle = Object.FindAnyObjectByType<BattleController>();
            yield return WaitFor(() => !battle.Busy);
            for (int i = 0; i < 8 && !battle.ResultShown; i++)
            {
                battle.EndTurn();
                yield return WaitFor(() => !battle.Busy || battle.ResultShown, 30f);
            }
            yield return WaitFor(() => battle.ResultShown, 30f);
            battle.Continue();

            yield return WaitScene("End");
            Assert.IsFalse(Object.FindAnyObjectByType<EndController>().Won);
            Assert.IsTrue(Click("Tentar de novo"));
            yield return WaitScene("Board");
            var run = GameManager.Instance.Run;
            Assert.AreEqual(50, run.Hp);
            Assert.AreEqual(0, run.Coins);
            Assert.AreEqual(10, run.Deck.Count);
            Assert.IsEmpty(run.Progress.Visited);
        }

        [UnityTest]
        public IEnumerator TelaFinal_VoltarAoTitulo()
        {
            var gm = GameManager.Ensure(Cfg());
            gm.EndRun(false);
            yield return WaitScene("End");
            Assert.IsTrue(Click("Voltar ao título"));
            yield return WaitScene("Title");
        }

        [UnityTest]
        public IEnumerator Pausa_CongelaOTempoEVoltaAoTitulo()
        {
            yield return StartAtBoard();
            var pause = Object.FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include);
            Assert.IsNotNull(pause);
            Assert.IsFalse(pause.IsPaused);
            pause.Pause();
            Assert.IsTrue(pause.IsPaused);
            Assert.AreEqual(0f, Time.timeScale);
            pause.Resume();
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(pause.IsPaused);

            pause.Pause();
            Assert.IsTrue(Click("Voltar ao título"));
            Assert.AreEqual(1f, Time.timeScale, "voltar ao título descongela o tempo");
            yield return WaitScene("Title");
            Assert.IsNull(GameManager.Instance.Run);
        }

        [UnityTest]
        public IEnumerator Titulo_JogarComecaUmaPartida()
        {
            SceneManager.LoadScene("Title");
            yield return WaitScene("Title");
            Assert.IsTrue(Click("Jogar"));
            yield return WaitScene("Board");
            Assert.IsNotNull(GameManager.Instance.Run);
            Assert.AreEqual(50, GameManager.Instance.Run.Hp);
        }

        [UnityTest]
        public IEnumerator ReiSlime_InvocaOsMirinsNaBatalhaDeVerdade()
        {
            yield return StartAtBoard();
            var board = Object.FindAnyObjectByType<BoardController>();
            board.speed = 40f;
            board.PlaceAt(10);
            board.Rng = new ScriptedRng(1);
            board.RollDice();
            yield return WaitScene("Battle");
            var battle = Object.FindAnyObjectByType<BattleController>();
            yield return WaitFor(() => !battle.Busy);
            battle.Sys.Hero.Hp = 500; battle.Sys.Hero.MaxHp = 500;      // sobrevive ao ciclo inteiro
            for (int i = 0; i < 2; i++)                                    // Esmagar, depois Dividir
            {
                battle.EndTurn();
                yield return WaitFor(() => battle.Busy);
                yield return WaitFor(() => !battle.Busy);
            }
            Assert.AreEqual(3, battle.Sys.Enemies.Count, "o Rei Slime invocou 2 Slimes Mirins");
            Assert.AreEqual(2, battle.Sys.Enemies.Count(e => e.Data.id == "slime_mirim"));
            Assert.AreEqual(4, Object.FindObjectsByType<ActorView>().Length, "herói + Rei + 2 Mirins");
        }

        [UnityTest]
        public IEnumerator Musica_TrocaPorCena_E_TransicaoClareiaDepois()
        {
            SceneManager.LoadScene("Title");
            yield return WaitScene("Title");
            GameManager.Ensure(Cfg());
            yield return null;
            Assert.AreEqual("board", AudioManager.CurrentTrackName);

            GameManager.Instance.StartRun();                     // com fade de saída
            yield return WaitScene("Board");
            Assert.AreEqual("board", AudioManager.CurrentTrackName);

            var board = Object.FindAnyObjectByType<BoardController>();
            yield return WaitFor(() => board.IntroVisible);
            Assert.IsTrue(Click("Começar"));                     // a história precisa ser fechada antes do dado
            yield return null;
            board.speed = 40f;
            board.Rng = new ScriptedRng(1);
            board.RollDice();                                    // casa 1 = batalha
            yield return WaitScene("Battle");
            Assert.AreEqual("battle", AudioManager.CurrentTrackName);

            // a tela volta a ficar visível depois do fade (o preto some)
            yield return WaitFor(() =>
            {
                var img = GameObject.Find("Preto")?.GetComponent<Image>();
                return img != null && img.color.a < 0.01f;
            });
        }
    }
}
