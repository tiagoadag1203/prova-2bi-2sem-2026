using UnityEngine;
using UnityEngine.SceneManagement;
using XequeDeTinta.Battle;
using XequeDeTinta.Data;

namespace XequeDeTinta.Core
{
    /// <summary>
    /// Persistente entre as cenas (Title, Board, Battle, End). Guarda o RunState e conduz as trocas de cena.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameConfig Config { get; private set; }
        public RunState Run { get; private set; }
        public IRng Rng { get; set; } = new SeededRng();
        /// <summary>Resultado da última partida (para a tela final do M6).</summary>
        public bool? LastRunWon { get; private set; }

        /// <summary>Acelera a apresentação das batalhas (playtest automático e testes).</summary>
        public float BattleSpeed;

        /// <summary>Devolve o GameManager existente ou cria um novo a partir do config. Não cria partida: use EnsureRun.</summary>
        public static GameManager Ensure(GameConfig config)
        {
            if (Instance == null)
            {
                var go = new GameObject("GameManager");
                DontDestroyOnLoad(go);
                go.AddComponent<GameManager>();
            }
            if (Instance.Config == null) Instance.Config = config;
            return Instance;
        }

        /// <summary>Garante que existe uma partida em andamento (o tabuleiro aberto direto no Editor cria uma nova).</summary>
        public RunState EnsureRun() => Run ?? NewRun();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ScreenFader.Get();
            AudioManager.Ensure();
        }

        /// <summary>Carrega a cena com um fade curto de saída (a entrada clareia sozinha).</summary>
        public void Load(string scene) => StartCoroutine(LoadRoutine(scene));

        System.Collections.IEnumerator LoadRoutine(string scene)
        {
            yield return ScreenFader.Get().FadeOut();
            SceneManager.LoadScene(scene);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Configure(GameConfig config) { Config = config; }

        public RunState NewRun()
        {
            Run = new RunState(Config, Rng);
            LastRunWon = null;
            return Run;
        }

        public void StartRun()
        {
            NewRun().IntroPending = true;
            Load("Board");
        }

        public void BackToTitle()
        {
            Run = null;
            Load("Title");
        }

        // ---------- Batalha ----------

        /// <summary>Lança a batalha da casa (Chefe usa o encontro do chefe) e carrega a cena Battle.</summary>
        public void StartBattle(BoardTile tile)
        {
            var encounter = tile.type == TileType.Boss ? Config.bossEncounter : Run.NextBattle();
            Run.PendingTileId = tile.id;
            BattleLauncher.Setup = new BattleSetup
            {
                HeroHp = Run.Hp,
                HeroMaxHp = Run.MaxHp,
                Deck = new System.Collections.Generic.List<CardInstance>(Run.Deck),
                Encounter = encounter,
                Rules = BattleRules.From(Config),
                Rng = new SeededRng(Rng.Range(0, int.MaxValue)),
                Relics = new System.Collections.Generic.List<RelicData>(Run.Relics),
            };
            BattleLauncher.SpeedOverride = BattleSpeed;
            BattleLauncher.OnFinished = r => OnBattleFinished(r, tile);
            Load("Battle");
        }

        void OnBattleFinished(BattleResult result, BoardTile tile)
        {
            Run.Hp = Mathf.Max(0, result.HeroHp);
            BattleLauncher.Clear();
            if (!result.Won) { EndRun(false); return; }
            if (tile.type == TileType.Boss) { EndRun(true); return; }
            Run.Reward = Run.RollReward();
            Load("Board");
        }

        /// <summary>Fim da partida. No M6 isto carrega a cena End (vitória ou derrota); por ora volta ao título.</summary>
        public void EndRun(bool won)
        {
            LastRunWon = won;
            Run = null;
            Load(Application.CanStreamedLevelBeLoaded("End") ? "End" : "Title");
        }
    }
}
