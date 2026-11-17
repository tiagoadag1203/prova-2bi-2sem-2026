using System.Collections.Generic;
using UnityEngine;

namespace XequeDeTinta.Data
{
    [CreateAssetMenu(menuName = "Xeque de Tinta/Encontro", fileName = "Encounter_Novo")]
    public class EncounterData : ScriptableObject
    {
        public string id;
        /// <summary>Da esquerda para a direita (máx. 3 em batalha comum).</summary>
        public List<EnemyData> enemies = new List<EnemyData>();
        public bool isBoss;
    }
}
