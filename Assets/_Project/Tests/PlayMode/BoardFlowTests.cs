using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using XequeDeTinta.Board;
using XequeDeTinta.Core;
using XequeDeTinta.Data;

namespace XequeDeTinta.Tests
{
    public class BoardFlowTests
    {
        BoardController board;

        [UnitySetUp]
        public IEnumerator Load()
        {
            SceneManager.LoadScene("Board");
            yield return null;
            yield return null;
            board = Object.FindFirstObjectByType<BoardController>();
            Assert.IsNotNull(board, "BoardController não encontrado na cena Board");
            board.speed = 40f;
        }

        IEnumerator WaitFor(System.Func<bool> cond, float timeout = 10f)
        {
            float t = 0;
            while (!cond() && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(cond(), "tempo esgotado esperando a condição");
        }

        [UnityTest]
        public IEnumerator RolarAnda_Bifurca_Escolhe_E_ResolveACasa()
        {
            var resolved = new System.Collections.Generic.List<TileType>();
            board.Resolver = (tile, done) => { resolved.Add(tile.type); done(); };
            board.Rng = new ScriptedRng(3);

            Assert.AreEqual(BoardState.Idle, board.State);
            board.RollDice();
            yield return WaitFor(() => board.State == BoardState.Choosing);
            Assert.AreEqual(3, board.LastRoll);
            Assert.AreEqual(2, board.CurrentTileId, "parou na bifurcação esperando a escolha");

            board.Choose(3);
            yield return WaitFor(() => board.State == BoardState.Idle);

            Assert.AreEqual(3, board.CurrentTileId);
            CollectionAssert.AreEqual(new[] { TileType.Battle }, resolved);
            Assert.IsTrue(board.IsTileVisited(3), "a casa resolvida fica cinza");
        }

        [UnityTest]
        public IEnumerator CasaJaVisitadaNaoResolveDeNovo()
        {
            int calls = 0;
            board.Resolver = (tile, done) => { calls++; done(); };

            board.Rng = new ScriptedRng(1);
            board.RollDice();
            yield return WaitFor(() => board.State == BoardState.Idle);
            Assert.AreEqual(1, calls);
            Assert.IsTrue(board.IsTileVisited(1));

            board.PlaceAt(0);
            board.Rng = new ScriptedRng(1);
            board.RollDice();
            yield return WaitFor(() => board.State == BoardState.Idle);
            Assert.AreEqual(1, board.CurrentTileId);
            Assert.AreEqual(1, calls, "parar numa casa cinza não faz nada");
        }

        [UnityTest]
        public IEnumerator ParaNoChefeEFinaliza()
        {
            board.Resolver = (tile, done) => done();
            board.PlaceAt(10);
            board.Rng = new ScriptedRng(6);
            board.RollDice();
            yield return WaitFor(() => board.State == BoardState.Finished);
            Assert.AreEqual(11, board.CurrentTileId);
        }

        [UnityTest]
        public IEnumerator NaoRolaDuasVezesEnquantoAnda()
        {
            board.Resolver = (tile, done) => done();
            board.Rng = new ScriptedRng(1, 6);
            board.RollDice();
            board.RollDice();                       // ignorado: já está rolando
            yield return WaitFor(() => board.State == BoardState.Idle);
            Assert.AreEqual(1, board.CurrentTileId);
        }
    }
}
