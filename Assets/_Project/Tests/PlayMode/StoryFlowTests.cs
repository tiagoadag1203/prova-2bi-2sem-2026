using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using XequeDeTinta.Board;
using XequeDeTinta.Core;
using XequeDeTinta.Data;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XequeDeTinta.Tests
{
    /// <summary>Tema "O Tabuleiro Manchado": caixa de história antes do primeiro dado e linha de premissa no título.</summary>
    public class StoryFlowTests
    {
        static GameConfig Cfg()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/_Project/Data/GameConfig.asset");
#else
            return null;
#endif
        }

        [SetUp] public void Reset() { if (GameManager.Instance != null) Object.DestroyImmediate(GameManager.Instance.gameObject); }
        [TearDown] public void Cleanup() { if (GameManager.Instance != null) Object.DestroyImmediate(GameManager.Instance.gameObject); }

        IEnumerator WaitScene(string name)
        {
            float t = 0;
            while (SceneManager.GetActiveScene().name != name && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.AreEqual(name, SceneManager.GetActiveScene().name);
            yield return null; yield return null;
        }

        static bool Click(string label)
        {
            foreach (var b in Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude))
            {
                var t = b.GetComponentInChildren<TMP_Text>();
                if (t != null && t.text == label && b.interactable) { b.onClick.Invoke(); return true; }
            }
            return false;
        }

        [Test]
        public void NomeDaFaseEOTabuleiroManchado() => Assert.AreEqual("O Tabuleiro Manchado", BoardController.StageName);

        [UnityTest]
        public IEnumerator Titulo_MostraAPremissaAbaixoDoLogo()
        {
            SceneManager.LoadScene("Title");
            yield return WaitScene("Title");
            var go = GameObject.Find("Premissa");
            Assert.IsNotNull(go);
            StringAssert.Contains("A tinta mágica afogou as peças", go.GetComponent<TMP_Text>().text);
            StringAssert.Contains("Só o peão pode salvar o tabuleiro", go.GetComponent<TMP_Text>().text);
        }

        [UnityTest]
        public IEnumerator PrimeiraAberturaDoTabuleiro_MostraHistoriaEBloqueiaODado()
        {
            SceneManager.LoadScene("Title");
            yield return WaitScene("Title");
            Assert.IsTrue(Click("Jogar"));
            yield return WaitScene("Board");
            var board = Object.FindAnyObjectByType<BoardController>();
            yield return null;
            Assert.IsTrue(board.IntroVisible, "a caixa de história aparece antes do dado");
            Assert.IsNotNull(GameObject.Find("PainelHistoria"));

            board.RollDice();                          // bloqueado enquanto a história está aberta
            yield return null;
            Assert.AreEqual(BoardState.Resolving, board.State);
            Assert.AreEqual(0, board.CurrentTileId);

            Assert.IsTrue(Click("Começar"));
            yield return null; yield return null;
            Assert.IsFalse(board.IntroVisible);
            Assert.AreEqual(BoardState.Idle, board.State);
            Assert.IsFalse(GameManager.Instance.Run.IntroPending);
            Assert.IsNull(GameObject.Find("PainelHistoria"));
        }

        [UnityTest]
        public IEnumerator HistoriaNaoVoltaAoRecarregarOTabuleiro()
        {
            SceneManager.LoadScene("Title");           // parte de uma cena diferente da Board (o teste anterior termina nela)
            yield return WaitScene("Title");
            var gm = GameManager.Ensure(Cfg());
            gm.StartRun();
            yield return WaitScene("Board");
            var first = Object.FindAnyObjectByType<BoardController>();
            float w = 0; while (!first.IntroVisible && w < 5f) { w += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(first.IntroVisible);
            if (!Click("Começar"))
            {
                var labels = new System.Text.StringBuilder();
                foreach (var b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include))
                    labels.Append('[').Append(b.GetComponentInChildren<TMP_Text>()?.text).Append(b.interactable ? "" : " (inativo)").Append(b.gameObject.activeInHierarchy ? "" : " (oculto)").Append("] ");
                Assert.Fail("botão Começar não achado; botões: " + labels);
            }
            yield return null;
            SceneManager.LoadScene("Board");           // volta de uma batalha, por exemplo
            yield return WaitScene("Board");
            Assert.IsFalse(Object.FindAnyObjectByType<BoardController>().IntroVisible);
        }

        [UnityTest]
        public IEnumerator PartidaDeTestePeloNewRun_NaoMostraHistoria()
        {
            GameManager.Ensure(Cfg()).NewRun();
            SceneManager.LoadScene("Board");
            yield return WaitScene("Board");
            Assert.IsFalse(Object.FindAnyObjectByType<BoardController>().IntroVisible);
        }
    }
}
