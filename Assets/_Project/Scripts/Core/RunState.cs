using System.Collections.Generic;
using XequeDeTinta.Battle;
using XequeDeTinta.Board;
using XequeDeTinta.Data;

namespace XequeDeTinta.Core
{
    /// <summary>Recompensa de uma batalha vencida: moedas e 3 cartas para escolher (ou pular).</summary>
    public class PendingReward
    {
        public int Coins;
        public List<CardData> Choices = new List<CardData>();
    }

    /// <summary>
    /// Estado da partida: vida, deck, relíquias, moedas, casa atual e casas visitadas.
    /// Lógica pura (sem MonoBehaviour); o GameManager guarda uma instância entre as cenas.
    /// </summary>
    public class RunState
    {
        public GameConfig Config { get; }
        /// <summary>Fonte de aleatoriedade da partida; os testes podem trocá-la por uma roteirizada.</summary>
        public IRng Rng { get; set; }

        public int Hp, MaxHp, Coins;
        public readonly List<CardInstance> Deck = new List<CardInstance>();
        public readonly List<RelicData> Relics = new List<RelicData>();
        public BoardProgress Progress { get; }

        /// <summary>Casa cuja resolução está em andamento (batalha em outra cena); -1 se nenhuma.</summary>
        public int PendingTileId = -1;
        public PendingReward Reward;
        /// <summary>A história de abertura ainda não foi mostrada nesta partida (só partidas iniciadas pelo título).</summary>
        public bool IntroPending;

        readonly Queue<EncounterData> battleQueue = new Queue<EncounterData>();
        readonly Queue<EventData> eventQueue = new Queue<EventData>();

        public RunState(GameConfig config, IRng rng)
        {
            Config = config; Rng = rng;
            MaxHp = Hp = config.heroMaxHp;
            foreach (var c in config.starterDeck) Deck.Add(new CardInstance(c));
            Progress = new BoardProgress(config.board.startTileId);
            RefillBattles();
            RefillEvents();
        }

        public bool IsAlive => Hp > 0;

        // ---------- Vida e moedas ----------

        public int Heal(int amount)
        {
            int healed = System.Math.Min(MaxHp - Hp, System.Math.Max(0, amount));
            Hp += healed;
            return healed;
        }

        public void LoseHp(int amount) => Hp = System.Math.Max(0, Hp - System.Math.Max(0, amount));

        public void AddCoins(int amount) => Coins += System.Math.Max(0, amount);

        public bool TrySpend(int amount)
        {
            if (amount < 0 || Coins < amount) return false;
            Coins -= amount;
            return true;
        }

        // ---------- Deck e relíquias ----------

        public CardInstance AddCard(CardData data, bool upgraded = false)
        {
            var c = new CardInstance(data, upgraded);
            Deck.Add(c);
            return c;
        }

        public bool RemoveCard(CardInstance card) => Deck.Remove(card);

        public bool UpgradeCard(CardInstance card)
        {
            if (!Deck.Contains(card) || card.Upgraded) return false;
            card.Upgraded = true;
            return true;
        }

        public void AddRelic(RelicData relic) { if (!Relics.Contains(relic)) Relics.Add(relic); }

        public bool HasRelic(RelicKind kind) => Relics.Exists(r => r.kind == kind);

        /// <summary>Relíquias do jogo que o jogador ainda não tem.</summary>
        public List<RelicData> MissingRelics() => Config.relics.FindAll(r => !Relics.Contains(r));

        // ---------- Sorteios (sempre pela semente da partida) ----------

        void RefillBattles()
        {
            var list = new List<EncounterData>(Config.battleEncounters);
            Rng.Shuffle(list);
            foreach (var e in list) battleQueue.Enqueue(e);
        }

        void RefillEvents()
        {
            var list = new List<EventData>(Config.events);
            Rng.Shuffle(list);
            foreach (var e in list) eventQueue.Enqueue(e);
        }

        /// <summary>Próximo grupo de batalha: os 5 grupos saem sem repetir.</summary>
        public EncounterData NextBattle()
        {
            if (battleQueue.Count == 0) RefillBattles();
            return battleQueue.Dequeue();
        }

        /// <summary>Próximo evento: 2 dos 3, sem repetir.</summary>
        public EventData NextEvent()
        {
            if (eventQueue.Count == 0) RefillEvents();
            return eventQueue.Dequeue();
        }

        public int RollCoins() => Rng.Range(Config.rewardCoinsMin, Config.rewardCoinsMax + 1);

        /// <summary>n cartas distintas do pool de recompensa, todas com a mesma chance.</summary>
        public List<CardData> RollCards(int n)
        {
            var pool = new List<CardData>(Config.rewardPool);
            Rng.Shuffle(pool);
            if (n < pool.Count) pool.RemoveRange(n, pool.Count - n);
            return pool;
        }

        public PendingReward RollReward() =>
            new PendingReward { Coins = RollCoins(), Choices = RollCards(Config.rewardCardChoices) };
    }
}
