using System.Collections.Generic;
using XequeDeTinta.Data;

namespace XequeDeTinta.Board
{
    /// <summary>Grafo das casas. Lógica pura, sem MonoBehaviour.</summary>
    public class BoardGraph
    {
        readonly Dictionary<int, BoardTile> tiles = new Dictionary<int, BoardTile>();

        public BoardGraph(IEnumerable<BoardTile> source)
        {
            foreach (var t in source) tiles[t.id] = t;
        }

        public IEnumerable<BoardTile> All => tiles.Values;
        public BoardTile Get(int id) => tiles[id];
        public bool Contains(int id) => tiles.ContainsKey(id);
        public IReadOnlyList<int> Next(int id) => tiles[id].next;
        public bool IsFork(int id) => tiles[id].next.Count > 1;
        public TileType TypeOf(int id) => tiles[id].type;
    }
}
