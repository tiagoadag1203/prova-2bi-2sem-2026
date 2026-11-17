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
    /// <summary>Casas do tabuleiro pela interface de verdade: cada teste clica nos botões dos painéis.</summary>
    public class TileFlowTests
    {
        BoardController board;
        RunState run;

        static GameConfig Cfg()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/_Project/Data/GameConfig.asset");
#else
            return null;
#endif
        }

        [UnitySetUp]
        public IEnumerator Load()
        {
            if (GameManager.Instance != null) Object.DestroyImmediate(GameManager.Instance.gameObject);
            BattleLauncher.Clear();
            var gm = GameManager.Ensure(Cfg());
            gm.BattleSpeed = 6f;
            run = gm.NewRun();
            SceneManager.LoadScene("Board");
            yield return null; yield return null;
            board = Object.FindAnyObjectByType<BoardController>();
            board.speed = 40f;
        }

        [TearDown]
        public void Cleanup()
        {
            if (GameManager.Instance != null) Object.DestroyImmediate(GameManager.Instance.gameObject);
            BattleLauncher.Clear();
        }

        IEnumerator WaitFor(System.Func<bool> cond, float timeout = 15f)
        {
            float t = 0;
            while (!cond() && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(cond(), "tempo esgotado esperando a condição");
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

        static bool ClickByName(string name)
        {
            foreach (var b in Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude))
                if (b.name == name && b.interactable) { b.onClick.Invoke(); return true; }
            return false;
        }

        /// <summary>Põe o peão em 'from', rola 'roll' e (se pedir bifurcação) escolhe 'branch'; espera a casa abrir o painel.</summary>
        IEnumerator GoTo(int from, int roll, int branch = -1)
        {
            board.PlaceAt(from);
            board.Rng = new ScriptedRng(roll);
            board.RollDice();
            if (branch >= 0)
            {
                yield return WaitFor(() => board.State == BoardState.Choosing);
                board.Choose(branch);
            }
            yield return WaitFor(() => board.TilePanel != null);
        }

        [UnityTest]
        public IEnumerator Descanso_Curar_RecuperaVidaEFechaACasa()
        {
            run.Hp = 30;
            yield return GoTo(2, 1, branch: 4);          // 2 -> escolhe a bifurcação -> 4 = Descanso (1 passo)
            Assert.AreEqual(4, board.CurrentTileId);
            Assert.IsTrue(Click("Curar 15 de vida"));
            Assert.AreEqual(45, run.Hp);
            Assert.IsTrue(Click("Continuar"));
            yield return WaitFor(() => board.State == BoardState.Idle);
            Assert.IsTrue(run.Progress.IsVisited(4));
            Assert.IsNull(board.TilePanel);
        }

        [UnityTest]
        public IEnumerator Descanso_MelhorarCarta_PelaInterface()
        {
            yield return GoTo(2, 1, branch: 4);
            Assert.IsTrue(Click("Melhorar 1 carta"));
            yield return null;
            var cards = Object.FindObjectsByType<CardView>();
            Assert.AreEqual(10, cards.Length, "o seletor mostra as 10 cartas melhoráveis");
            var alvo = cards.First(c => c.Card.Data.id == "golpe");
            alvo.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
            Assert.IsTrue(alvo.Card.Upgraded);
            Assert.AreEqual(1, run.Deck.Count(c => c.Upgraded));
            Assert.IsTrue(Click("Continuar"));
            yield return WaitFor(() => board.State == BoardState.Idle);
        }

        [UnityTest]
        public IEnumerator Bau_Da30MoedasERelicaEApareceNaBarra()
        {
            yield return GoTo(6, 1, branch: 8);          // 6 -> escolhe a bifurcação -> 8 = Baú (1 passo)
            Assert.AreEqual(8, board.CurrentTileId);
            Assert.AreEqual(30, run.Coins);
            Assert.AreEqual(1, run.Relics.Count);
            Assert.IsTrue(Click("Continuar"));
            yield return WaitFor(() => board.State == BoardState.Idle);
            Assert.IsNotNull(GameObject.Find("Relica_" + run.Relics[0].id), "a relíquia aparece no HUD");
        }

        [UnityTest]
        public IEnumerator Loja_ComprarUmaCarta_E_Sair()
        {
            run.AddCoins(200);
            yield return GoTo(5, 1);
            Assert.AreEqual(6, board.CurrentTileId);
            Assert.IsTrue(Click("Comprar 50"));
            Assert.AreEqual(150, run.Coins);
            Assert.AreEqual(11, run.Deck.Count);
            Assert.IsTrue(Click("Sair da loja"));
            yield return WaitFor(() => board.State == BoardState.Idle);
            Assert.IsTrue(run.Progress.IsVisited(6));
        }

        [UnityTest]
        public IEnumerator Loja_SemMoedasNaoCompra()
        {
            yield return GoTo(5, 1);
            Assert.IsTrue(Click("Comprar 50"));
            Assert.AreEqual(0, run.Coins);
            Assert.AreEqual(10, run.Deck.Count);
            Assert.IsTrue(Click("Sair da loja"));
            yield return WaitFor(() => board.State == BoardState.Idle);
        }

        [UnityTest]
        public IEnumerator Evento_EscolheEFecha()
        {
            run.Hp = 30;
            yield return GoTo(1, 1);                    // casa 2 = Evento
            Assert.AreEqual(2, board.CurrentTileId);
            Assert.IsTrue(ClickByName("EscolhaA"));
            yield return null;
            Assert.IsTrue(Click("Continuar"));
            yield return WaitFor(() => board.State == BoardState.Idle);
            Assert.IsTrue(run.Progress.IsVisited(2));
            Assert.IsNull(board.TilePanel);
        }

        [UnityTest]
        public IEnumerator DadoViciado_PodeRolarDeNovoUmaVez()
        {
            run.AddRelic(Cfg().relics.First(r => r.kind == RelicKind.RerollDice));
            board.PlaceAt(0);
            board.Resolver = (t, done) => done();
            board.Rng = new ScriptedRng(3, 1);
            int rolls = 0;
            board.RollFinished += _ => rolls++;
            board.RollDice();
            yield return WaitFor(() => board.State == BoardState.Confirming);
            Assert.AreEqual(3, board.LastRoll);
            Assert.IsTrue(Click("Rolar de novo"));
            yield return WaitFor(() => board.State == BoardState.Idle || board.State == BoardState.Resolving);
            yield return WaitFor(() => board.State == BoardState.Idle);
            Assert.AreEqual(2, rolls);
            Assert.AreEqual(1, board.LastRoll);
            Assert.AreEqual(1, board.CurrentTileId, "andou o valor do segundo dado");
        }

        [UnityTest]
        public IEnumerator DadoViciado_AndarMantemARolagem()
        {
            run.AddRelic(Cfg().relics.First(r => r.kind == RelicKind.RerollDice));
            board.Resolver = (t, done) => done();
            board.Rng = new ScriptedRng(1, 6);
            board.RollDice();
            yield return WaitFor(() => board.State == BoardState.Confirming);
            Assert.IsTrue(Click("Andar (Espaço)"));
            yield return WaitFor(() => board.State == BoardState.Idle);
            Assert.AreEqual(1, board.CurrentTileId);
        }

        [UnityTest]
        public IEnumerator SemDadoViciadoNaoPerguntaNada()
        {
            board.Resolver = (t, done) => done();
            board.Rng = new ScriptedRng(1);
            board.RollDice();
            yield return WaitFor(() => board.State == BoardState.Idle);
            Assert.AreEqual(1, board.CurrentTileId);
        }

        [UnityTest]
        public IEnumerator RelicaDeEscudoChegaNaBatalhaPeloGameManager()
        {
            run.AddRelic(Cfg().relics.First(r => r.kind == RelicKind.StartShield));
            board.Rng = new ScriptedRng(1);
            board.RollDice();                                    // casa 1 = Batalha
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "Battle");
            yield return null; yield return null;
            var battle = Object.FindAnyObjectByType<BattleController>();
            Assert.AreEqual(5, battle.Sys.Hero.Shield);
        }
    }
}
