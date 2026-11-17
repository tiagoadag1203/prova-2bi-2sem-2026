using System.Collections.Generic;
using XequeDeTinta.Data;

namespace XequeDeTinta.Board
{
    public enum StepKind
    {
        /// <summary>Andou uma casa.</summary>
        Moved,
        /// <summary>Bifurcação: o jogador precisa chamar Choose.</summary>
        ChoiceNeeded,
        /// <summary>O movimento acabou (passos esgotados, casa do Chefe ou fim do caminho).</summary>
        Stopped,
    }

    public readonly struct Step
    {
        public readonly StepKind kind;
        public readonly int tileId;
        public readonly IReadOnlyList<int> options;

        public Step(StepKind kind, int tileId, IReadOnlyList<int> options = null)
        {
            this.kind = kind; this.tileId = tileId; this.options = options;
        }
    }

    /// <summary>Anda o peão casa por casa. Para na casa do Chefe mesmo sobrando passos.</summary>
    public class BoardMover
    {
        readonly BoardGraph graph;

        public int Current { get; private set; }
        public int Remaining { get; private set; }
        public bool WaitingForChoice { get; private set; }
        public IReadOnlyList<int> Options => graph.Next(Current);

        public BoardMover(BoardGraph graph, int startTile)
        {
            this.graph = graph;
            Current = startTile;
        }

        public void Begin(int steps)
        {
            Remaining = steps;
            WaitingForChoice = false;
        }

        /// <summary>Próximo passo. Se devolver ChoiceNeeded, chame Choose antes de continuar.</summary>
        public Step Advance()
        {
            if (WaitingForChoice) return new Step(StepKind.ChoiceNeeded, Current, Options);
            if (Remaining <= 0) return new Step(StepKind.Stopped, Current);

            var next = graph.Next(Current);
            if (next.Count == 0) { Remaining = 0; return new Step(StepKind.Stopped, Current); }
            if (next.Count > 1)
            {
                WaitingForChoice = true;
                return new Step(StepKind.ChoiceNeeded, Current, next);
            }
            return MoveTo(next[0]);
        }

        /// <summary>Resolve uma bifurcação. Devolve o passo dado, ou Stopped se a opção for inválida.</summary>
        public Step Choose(int tileId)
        {
            if (!WaitingForChoice || !Contains(Options, tileId)) return new Step(StepKind.Stopped, Current);
            WaitingForChoice = false;
            return MoveTo(tileId);
        }

        Step MoveTo(int tileId)
        {
            Current = tileId;
            Remaining--;
            if (graph.TypeOf(tileId) == TileType.Boss) Remaining = 0;
            return new Step(StepKind.Moved, tileId);
        }

        static bool Contains(IReadOnlyList<int> list, int id)
        {
            for (int i = 0; i < list.Count; i++) if (list[i] == id) return true;
            return false;
        }
    }
}
