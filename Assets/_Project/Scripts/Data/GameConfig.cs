using System.Collections.Generic;
using UnityEngine;

namespace XequeDeTinta.Data
{
    /// <summary>Todos os números de balanceamento do jogo. Nada disso fica fixo no código.</summary>
    [CreateAssetMenu(menuName = "Xeque de Tinta/Configuração do jogo", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Herói")]
        public int heroMaxHp = 50;
        public List<CardData> starterDeck = new List<CardData>();

        [Header("Batalha")]
        public int energyPerTurn = 3;
        public int handSize = 5;
        public float effectiveMultiplier = 1.5f;
        public float weakMultiplier = 0.75f;
        public int burnDamage = 2;

        [Header("Tabuleiro")]
        public int diceSides = 3;
        /// <summary>Dado do evento Dado Gigante (1-2, 3-4, 5-6).</summary>
        public int eventDiceSides = 6;
        public float restHealFraction = 0.3f;

        [Header("Recompensas")]
        public int rewardCoinsMin = 10;
        public int rewardCoinsMax = 15;
        public int rewardCardChoices = 3;
        public int chestCoins = 30;

        [Header("Loja")]
        public int shopCardCount = 3;
        public int shopCardPrice = 50;
        public int shopRelicPrice = 100;
        public int shopHealAmount = 15;
        public int shopHealPrice = 35;
        public int shopRemoveCardPrice = 60;

        [Header("Conteúdo")]
        public List<CardData> rewardPool = new List<CardData>();
        public List<RelicData> relics = new List<RelicData>();
        public List<EncounterData> battleEncounters = new List<EncounterData>();
        public EncounterData bossEncounter;
        public List<EventData> events = new List<EventData>();
        public BoardLayout board;
    }
}
