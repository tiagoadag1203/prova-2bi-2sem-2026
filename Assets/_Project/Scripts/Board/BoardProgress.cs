using System.Collections.Generic;

namespace XequeDeTinta.Board
{
    /// <summary>Casa atual e casas visitadas (resolvidas). No M4 isto passa a viver no RunState.</summary>
    public class BoardProgress
    {
        readonly HashSet<int> visited = new HashSet<int>();

        public int CurrentTile { get; set; }
        public IReadOnlyCollection<int> Visited => visited;

        public BoardProgress(int startTile) { CurrentTile = startTile; }

        public bool IsVisited(int id) => visited.Contains(id);

        /// <summary>Marca a casa como resolvida (fica cinza). Devolve false se já estava.</summary>
        public bool MarkVisited(int id) => visited.Add(id);
    }

    public static class DiceRoller
    {
        public static int Roll(Core.IRng rng, int sides) => rng.Range(1, sides + 1);
    }
}
