using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using XequeDeTinta.Board;
using XequeDeTinta.Core;
using XequeDeTinta.Data;

namespace XequeDeTinta.Tests
{
    public class BoardLogicTests
    {
        static BoardLayout Layout() =>
            AssetDatabase.LoadAssetAtPath<BoardLayout>("Assets/_Project/Data/Boards/Board_Floresta.asset");

        static BoardMover NewMover(int start = 0) =>
            new BoardMover(new BoardGraph(Layout().tiles), start);

        /// <summary>Anda até parar, escolhendo sempre a opção de índice 'pick' nas bifurcações. Devolve as casas pisadas.</summary>
        static List<int> Walk(BoardMover m, int steps, int pick = 0)
        {
            var trail = new List<int>();
            m.Begin(steps);
            while (true)
            {
                var s = m.Advance();
                if (s.kind == StepKind.Stopped) break;
                if (s.kind == StepKind.ChoiceNeeded) { s = m.Choose(s.options[pick]); }
                trail.Add(s.tileId);
            }
            return trail;
        }

        [Test]
        public void AndaExatamenteOsPassosDoDado()
        {
            var m = NewMover(0);
            Assert.AreEqual(new[] { 1, 2 }, Walk(m, 2).ToArray());
            Assert.AreEqual(2, m.Current);
        }

        [Test]
        public void ZeroPassosNaoAnda()
        {
            var m = NewMover(0);
            Assert.IsEmpty(Walk(m, 0));
            Assert.AreEqual(0, m.Current);
        }

        [Test]
        public void BifurcacaoPedeEscolhaQuandoSobramPassos()
        {
            var m = NewMover(1);
            m.Begin(3);
            Assert.AreEqual(StepKind.Moved, m.Advance().kind);        // 1 -> 2
            var s = m.Advance();                                       // 2 é bifurcação
            Assert.AreEqual(StepKind.ChoiceNeeded, s.kind);
            CollectionAssert.AreEquivalent(new[] { 3, 4 }, s.options.ToArray());
            Assert.IsTrue(m.WaitingForChoice);
            Assert.AreEqual(StepKind.ChoiceNeeded, m.Advance().kind); // continua pedindo até escolher
            Assert.AreEqual(3, m.Choose(3).tileId);
            Assert.AreEqual(1, m.Remaining);
        }

        [Test]
        public void ParaNaBifurcacaoSeOsPassosAcabamLa()
        {
            var m = NewMover(1);
            var trail = Walk(m, 1);
            Assert.AreEqual(new[] { 2 }, trail.ToArray());
            Assert.AreEqual(StepKind.Stopped, m.Advance().kind);
            Assert.IsFalse(m.WaitingForChoice);
        }

        [Test]
        public void EscolhaInvalidaNaoAnda()
        {
            var m = NewMover(2);
            m.Begin(2);
            Assert.AreEqual(StepKind.ChoiceNeeded, m.Advance().kind);
            Assert.AreEqual(StepKind.Stopped, m.Choose(9).kind);
            Assert.AreEqual(2, m.Current);
        }

        [Test]
        public void EscolherSemPerguntarNaoFunciona()
        {
            var m = NewMover(0);
            m.Begin(2);
            Assert.AreEqual(StepKind.Stopped, m.Choose(1).kind);
            Assert.AreEqual(0, m.Current);
        }

        [Test]
        public void OsDoisRamosSeJuntamNaMesmaCasa()
        {
            var a = NewMover(2); Walk(a, 2, pick: 0);
            var b = NewMover(2); Walk(b, 2, pick: 1);
            Assert.AreEqual(5, a.Current);
            Assert.AreEqual(5, b.Current);
        }

        [Test]
        public void ParaNoChefeMesmoSobrandoPassos()
        {
            var m = NewMover(9);
            var trail = Walk(m, 6);
            Assert.AreEqual(new[] { 10, 11 }, trail.ToArray());
            Assert.AreEqual(11, m.Current);
            Assert.AreEqual(0, m.Remaining);
        }

        [Test]
        public void NoChefeNaoAndaMais()
        {
            var m = NewMover(11);
            Assert.IsEmpty(Walk(m, 4));
            Assert.AreEqual(11, m.Current);
        }

        [Test]
        public void QualquerDadoDoInicioChegaAoChefeEmNoMaximoAlgumasRodadas()
        {
            // Com dado 1..6 e 11 casas até o Chefe, nenhum caminho passa do Chefe.
            for (int roll = 1; roll <= 6; roll++)
                foreach (int pick in new[] { 0, 1 })
                {
                    var m = NewMover(0);
                    for (int turn = 0; turn < 12 && m.Current != 11; turn++) Walk(m, roll, pick);
                    Assert.AreEqual(11, m.Current, $"dado fixo {roll}, caminho {pick}");
                }
        }

        [Test]
        public void ProgressoMarcaCasasVisitadas()
        {
            var p = new BoardProgress(0);
            Assert.IsFalse(p.IsVisited(3));
            Assert.IsTrue(p.MarkVisited(3));
            Assert.IsTrue(p.IsVisited(3));
            Assert.IsFalse(p.MarkVisited(3), "segunda marcação não conta");
            Assert.AreEqual(1, p.Visited.Count);
        }

        [Test]
        public void DadoUsaGeradorInjetado()
        {
            var rng = new ScriptedRng(1, 4, 6);              // o roteiro dita as faces
            Assert.AreEqual(1, DiceRoller.Roll(rng, 6));
            Assert.AreEqual(4, DiceRoller.Roll(rng, 6));
            Assert.AreEqual(6, DiceRoller.Roll(rng, 6));
        }

        [Test]
        public void DadoComMesmaSementeRepete()
        {
            var a = new SeededRng(42); var b = new SeededRng(42);
            for (int i = 0; i < 20; i++)
            {
                int x = DiceRoller.Roll(a, 6);
                Assert.AreEqual(x, DiceRoller.Roll(b, 6));
                Assert.That(x, Is.InRange(1, 6));
            }
        }

        [Test]
        public void EmbaralharMantemOsMesmosElementos()
        {
            var list = Enumerable.Range(0, 10).ToList();
            new SeededRng(7).Shuffle(list);
            CollectionAssert.AreEquivalent(Enumerable.Range(0, 10), list);
        }
    }
}
