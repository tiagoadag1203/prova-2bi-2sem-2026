using System;
using System.Collections.Generic;
using UnityEngine;

namespace XequeDeTinta.Data
{
    [Serializable]
    public class BoardTile
    {
        public int id;
        public TileType type;
        public Vector2 position;
        /// <summary>Casas seguintes. Mais de uma = bifurcação.</summary>
        public List<int> next = new List<int>();
    }

    [CreateAssetMenu(menuName = "Xeque de Tinta/Layout do tabuleiro", fileName = "Board_Novo")]
    public class BoardLayout : ScriptableObject
    {
        public List<BoardTile> tiles = new List<BoardTile>();
        public int startTileId;

        public BoardTile Get(int id) => tiles.Find(t => t.id == id);
    }
}
