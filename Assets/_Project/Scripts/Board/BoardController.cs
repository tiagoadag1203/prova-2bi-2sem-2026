using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using XequeDeTinta.Core;
using XequeDeTinta.Data;
using XequeDeTinta.UI;

namespace XequeDeTinta.Board
{
    public enum BoardState { Idle, Rolling, Confirming, Moving, Choosing, Resolving, Finished }

    /// <summary>
    /// Ponte entre a lógica do tabuleiro (BoardGraph/BoardMover/BoardProgress) e a cena:
    /// desenha casas, peão e dado, trata cliques e teclas, e anima o movimento.
    /// </summary>
    public class BoardController : MonoBehaviour
    {
        [Header("Dados")]
        public GameConfig config;

        [Header("Arte (ligada pelo construtor da cena)")]
        public Sprite[] tileSprites;      // indexado por TileType
        public Sprite[] diceFaces;        // faces 1..6 nos índices 0..5
        public Sprite pawnSprite;
        public RuntimeAnimatorController pawnAnimator;

        [Header("Ritmo")]
        public float hopSeconds = 0.28f;
        public float rollSeconds = 0.8f;
        public float speed = 1f;          // os testes aceleram isto

        public IRng Rng = new SeededRng();
        /// <summary>Quem resolve a casa onde o peão parou (M4/M5 ligam batalha, loja...). Chame 'done' ao terminar.</summary>
        public Action<BoardTile, Action> Resolver;
        public event Action<int> RollFinished;
        public event Action DeckRequested;

        public BoardState State { get; private set; } = BoardState.Idle;
        public BoardProgress Progress { get; private set; }
        public int LastRoll { get; private set; }
        public int CurrentTileId => Progress.CurrentTile;
        public string Message => messageText != null ? messageText.text : "";

        public RunState Run { get; private set; }
        /// <summary>Painel de recompensa visível (esperando o jogador escolher).</summary>
        public bool RewardVisible => rewardPanel != null;
        GameObject rewardPanel, deckPanel;

        BoardGraph graph;
        BoardMover mover;
        readonly Dictionary<int, SpriteRenderer> tileRenderers = new Dictionary<int, SpriteRenderer>();
        readonly List<Transform> markers = new List<Transform>();
        Transform pawn;
        Animator pawnAnim;
        SpriteRenderer diceRenderer;
        TextMeshProUGUI statusText, coinText, messageText;
        Button rollButton, walkButton, rerollButton;
        RelicBar relicBar;
        int rollDecision;           // 0 esperando, 1 andar, 2 rolar de novo
        Canvas hudCanvas;
        Camera cam;
        int chosen = -1;
        Sprite dotSprite, arrowSprite;

        static readonly Color VisitedTint = new Color(0.42f, 0.42f, 0.48f, 1f);

        void Start()
        {
            cam = Camera.main;
            graph = new BoardGraph(config.board.tiles);
            var gm = GameManager.Ensure(config);
            Run = gm.EnsureRun();
            Progress = Run.Progress;
            mover = new BoardMover(graph, Progress.CurrentTile);
            dotSprite = MakeDot();
            arrowSprite = MakeArrow();
            BuildBackground();
            BuildTiles();
            BuildPawn();
            BuildDice();
            BuildHud();
            PauseMenu.Attach(hudCanvas.transform, () => GameManager.Instance?.BackToTitle());
            SetState(BoardState.Idle);
            SetMessage("Role o dado para andar!");
            foreach (var id in Progress.Visited) tileRenderers[id].color = VisitedTint;
            if (Run.PendingTileId >= 0) StartCoroutine(ResumeAfterBattle());
        }

        // ---------- Construção ----------

        void BuildBackground()
        {
            var art = UIArt.Get();
            if (art == null || art.backgroundBoard == null) return;
            var bg = new GameObject("Fundo", typeof(SpriteRenderer));
            bg.transform.SetParent(transform, false);
            var sr = bg.GetComponent<SpriteRenderer>();
            sr.sprite = art.backgroundBoard; sr.sortingOrder = -20;
            // Escurece para as casas e a interface se destacarem.
            var dim = new GameObject("Escurecer", typeof(SpriteRenderer));
            dim.transform.SetParent(transform, false);
            var ds = dim.GetComponent<SpriteRenderer>();
            ds.sprite = MakeDot(); ds.color = new Color(0.1f, 0.05f, 0.18f, 0.4f); ds.sortingOrder = -19;
            dim.transform.localScale = new Vector3(40f * 32f, 24f * 32f, 1f);
        }

        void BuildTiles()
        {
            var root = new GameObject("Casas").transform;
            root.SetParent(transform, false);

            // Ligações: pontinhos entre as casas.
            foreach (var t in graph.All)
                foreach (var n in t.next)
                {
                    Vector2 a = t.position, b = graph.Get(n).position;
                    float len = Vector2.Distance(a, b);
                    int count = Mathf.Max(2, Mathf.RoundToInt(len * 32f / 6f));
                    for (int i = 1; i < count; i++)
                    {
                        var d = new GameObject("Ponto", typeof(SpriteRenderer));
                        d.transform.SetParent(root, false);
                        d.transform.position = Vector2.Lerp(a, b, i / (float)count);
                        d.transform.localScale = new Vector3(3, 3, 1);
                        var sr = d.GetComponent<SpriteRenderer>();
                        sr.sprite = dotSprite; sr.color = Palette.Cream; sr.sortingOrder = -1;
                    }
                }

            foreach (var t in graph.All)
            {
                var go = new GameObject("Casa_" + t.id + "_" + t.type, typeof(SpriteRenderer), typeof(BoxCollider2D));
                go.transform.SetParent(root, false);
                go.transform.position = t.position;
                var sr = go.GetComponent<SpriteRenderer>();
                sr.sprite = tileSprites[(int)t.type];
                sr.sortingOrder = 0;
                go.GetComponent<BoxCollider2D>().size = Vector2.one;
                tileRenderers[t.id] = sr;
            }
        }

        void BuildPawn()
        {
            var go = new GameObject("Peao", typeof(SpriteRenderer), typeof(Animator));
            go.transform.SetParent(transform, false);
            go.transform.localScale = new Vector3(2, 2, 1);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = pawnSprite; sr.sortingOrder = 3;
            pawnAnim = go.GetComponent<Animator>();
            pawnAnim.runtimeAnimatorController = pawnAnimator;
            pawn = go.transform;
            pawn.position = PawnPosition(graph.Get(Progress.CurrentTile).position);
        }

        static Vector3 PawnPosition(Vector2 tilePos) => new Vector3(tilePos.x, tilePos.y - 0.3f, 0f);

        void BuildDice()
        {
            var go = new GameObject("Dado", typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(0f, -3.4f, 0f);
            go.transform.localScale = new Vector3(2, 2, 1);
            diceRenderer = go.GetComponent<SpriteRenderer>();
            diceRenderer.sprite = diceFaces[0];
            diceRenderer.sortingOrder = 2;
        }

        void BuildHud()
        {
            var canvas = UIFactory.CreateCanvas("HUD", 10, transform);
            hudCanvas = canvas;
            UIFactory.EnsureEventSystem();

            statusText = UIFactory.CreateText("Status", canvas.transform, "", 8, Palette.Cream, TextAlignmentOptions.TopLeft, false);
            statusText.rectTransform.anchorMin = new Vector2(0, 1); statusText.rectTransform.anchorMax = new Vector2(0, 1);
            statusText.rectTransform.pivot = new Vector2(0, 1);
            statusText.rectTransform.sizeDelta = new Vector2(100, 14);
            statusText.rectTransform.anchoredPosition = new Vector2(8, -8);

            var art = UIArt.Get();
            if (art != null && art.coin != null)
            {
                var ic = UIFactory.Anchored("IconeMoeda", canvas.transform, new Vector2(0, 1), new Vector2(16, 16), new Vector2(112, -6));
                ic.pivot = new Vector2(0, 1);
                var img = ic.gameObject.AddComponent<Image>(); img.sprite = art.coin; img.raycastTarget = false; img.preserveAspect = true;
            }
            coinText = UIFactory.CreateText("Moedas", canvas.transform, "", 8, Palette.Gold, TextAlignmentOptions.TopLeft, false);
            coinText.rectTransform.anchorMin = coinText.rectTransform.anchorMax = new Vector2(0, 1);
            coinText.rectTransform.pivot = new Vector2(0, 1);
            coinText.rectTransform.sizeDelta = new Vector2(60, 14);
            coinText.rectTransform.anchoredPosition = new Vector2(132, -8);

            messageText = UIFactory.CreateText("Mensagem", canvas.transform, "", 8, Palette.Gold, TextAlignmentOptions.Top);
            messageText.rectTransform.anchorMin = new Vector2(0.5f, 1); messageText.rectTransform.anchorMax = new Vector2(0.5f, 1);
            messageText.rectTransform.pivot = new Vector2(0.5f, 1);
            messageText.rectTransform.sizeDelta = new Vector2(420, 24);
            messageText.rectTransform.anchoredPosition = new Vector2(0, -30);

            rollButton = UIFactory.CreateButton("Rolar", canvas.transform, "Rolar dado (Espaço)",
                new Vector2(0.5f, 0), new Vector2(200, 20), new Vector2(0, 8), RollDice);
            walkButton = UIFactory.CreateButton("Andar", canvas.transform, "Andar (Espaço)",
                new Vector2(0.5f, 0), new Vector2(130, 20), new Vector2(-70, 8), ConfirmRoll);
            rerollButton = UIFactory.CreateButton("RolarDeNovo", canvas.transform, "Rolar de novo",
                new Vector2(0.5f, 0), new Vector2(130, 20), new Vector2(70, 8), RerollOnce);
            walkButton.gameObject.SetActive(false); rerollButton.gameObject.SetActive(false);
            relicBar = RelicBar.Create(canvas.transform);
            UIFactory.CreateButton("VerDeck", canvas.transform, "Ver deck",
                new Vector2(1, 1), new Vector2(70, 18), new Vector2(-8, -8), OpenDeck);
            RefreshHud();
        }

        public void RefreshHud()
        {
            if (statusText != null && Run != null) statusText.text = $"Vida {Run.Hp}/{Run.MaxHp}";
            if (coinText != null && Run != null) coinText.text = Run.Coins.ToString();
            if (relicBar != null && Run != null) relicBar.Rebuild(Run.Relics);
        }

        void OpenDeck()
        {
            if (deckPanel != null || rewardPanel != null) return;
            DeckRequested?.Invoke();
            deckPanel = Panels.Deck(hudCanvas.transform, Run.Deck, () => { Destroy(deckPanel); deckPanel = null; });
        }

        void SetMessage(string text) { if (messageText != null) messageText.text = text; }

        void SetState(BoardState s)
        {
            State = s;
            if (rollButton != null) rollButton.interactable = s == BoardState.Idle;
        }

        /// <summary>Põe o peão numa casa sem animar (testes e retomada de partida).</summary>
        public void PlaceAt(int tileId)
        {
            Progress.CurrentTile = tileId;
            mover = new BoardMover(graph, tileId);
            if (pawn != null) pawn.position = PawnPosition(graph.Get(tileId).position);
        }

        public bool IsTileVisited(int tileId) => Progress.IsVisited(tileId);

        // ---------- Entrada ----------

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if (State == BoardState.Idle) RollDice();
                else if (State == BoardState.Confirming) ConfirmRoll();
            }

            if (State == BoardState.Choosing)
            {
                for (int i = 0; i < markers.Count; i++)
                {
                    var p = markers[i].position;
                    p.y = graph.Get(mover.Options[i]).position.y + 0.95f + Mathf.Sin(Time.time * 6f) * 0.08f;
                    markers[i].position = p;
                }
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && cam != null)
                {
                    Vector2 world = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                    foreach (var id in mover.Options)
                        if (Vector2.Distance(world, graph.Get(id).position) < 0.8f) { Choose(id); break; }
                }
            }
        }

        // ---------- Fluxo ----------

        public void RollDice()
        {
            if (State != BoardState.Idle) return;
            StartCoroutine(RollRoutine());
        }

        public void Choose(int tileId)
        {
            if (State == BoardState.Choosing && mover.Options.Contains(tileId)) chosen = tileId;
        }

        float Dur(float seconds) => seconds / Mathf.Max(0.01f, speed);

        public void ConfirmRoll() { if (State == BoardState.Confirming) rollDecision = 1; }
        public void RerollOnce() { if (State == BoardState.Confirming) rollDecision = 2; }

        IEnumerator RollOnce()
        {
            SetState(BoardState.Rolling);
            SetMessage("Rolando...");
            float t = 0;
            while (t < Dur(rollSeconds))
            {
                diceRenderer.sprite = diceFaces[UnityEngine.Random.Range(0, diceFaces.Length)];
                diceRenderer.transform.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-12f, 12f));
                float step = Dur(0.06f);
                yield return new WaitForSeconds(step);
                t += step;
            }
            diceRenderer.transform.localRotation = Quaternion.identity;
            LastRoll = DiceRoller.Roll(Rng, config.diceSides);
            diceRenderer.sprite = diceFaces[Mathf.Clamp(LastRoll, 1, diceFaces.Length) - 1];
            SetMessage($"Tirou {LastRoll}!");
            RollFinished?.Invoke(LastRoll);
            yield return new WaitForSeconds(Dur(0.25f));
        }

        IEnumerator RollRoutine()
        {
            yield return RollOnce();

            // Dado Viciado: pode rolar de novo 1 vez por rolagem (provisório).
            if (Run.HasRelic(RelicKind.RerollDice))
            {
                SetState(BoardState.Confirming);
                SetMessage($"Tirou {LastRoll}! Andar ou rolar de novo?");
                rollDecision = 0;
                walkButton.gameObject.SetActive(true); rerollButton.gameObject.SetActive(true);
                rollButton.gameObject.SetActive(false);
                while (rollDecision == 0) yield return null;
                walkButton.gameObject.SetActive(false); rerollButton.gameObject.SetActive(false);
                rollButton.gameObject.SetActive(true);
                if (rollDecision == 2) yield return RollOnce();
            }

            SetState(BoardState.Moving);
            mover.Begin(LastRoll);
            while (true)
            {
                var step = mover.Advance();
                if (step.kind == StepKind.ChoiceNeeded)
                {
                    yield return ChooseRoutine();
                    step = mover.Choose(chosen);
                    SetState(BoardState.Moving);
                }
                if (step.kind == StepKind.Stopped) break;
                yield return Hop(step.tileId);
                Progress.CurrentTile = step.tileId;
            }

            Progress.CurrentTile = mover.Current;
            yield return ResolveRoutine(graph.Get(mover.Current));
        }

        IEnumerator ChooseRoutine()
        {
            SetState(BoardState.Choosing);
            SetMessage("Escolha o caminho: clique na casa");
            chosen = -1;
            ClearMarkers();
            foreach (var id in mover.Options)
            {
                var m = new GameObject("Seta", typeof(SpriteRenderer));
                m.transform.SetParent(transform, false);
                m.transform.localScale = new Vector3(2, 2, 1);
                var sr = m.GetComponent<SpriteRenderer>();
                sr.sprite = arrowSprite; sr.color = Palette.Gold; sr.sortingOrder = 5;
                markers.Add(m.transform);
            }
            while (chosen < 0) yield return null;
            ClearMarkers();
        }

        void ClearMarkers()
        {
            foreach (var m in markers) if (m != null) Destroy(m.gameObject);
            markers.Clear();
        }

        IEnumerator Hop(int tileId)
        {
            Vector3 from = pawn.position, to = PawnPosition(graph.Get(tileId).position);
            if (pawnAnim != null) { pawnAnim.ResetTrigger("hop"); pawnAnim.SetTrigger("hop"); }
            float d = Dur(hopSeconds), t = 0;
            while (t < d)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / d);
                var p = Vector3.Lerp(from, to, k);
                p.y += Mathf.Sin(k * Mathf.PI) * 0.45f;
                pawn.position = p;
                yield return null;
            }
            pawn.position = to;
        }

        IEnumerator ResolveRoutine(BoardTile tile)
        {
            SetState(BoardState.Resolving);
            if (Progress.IsVisited(tile.id) || tile.type == TileType.Start)
            {
                SetMessage(tile.type == TileType.Start ? "De volta ao início." : "Casa já resolvida: nada acontece.");
            }
            else
            {
                bool done = false;
                SetMessage(Describe(tile.type));
                if (Resolver != null) Resolver(tile, () => done = true);
                else DefaultResolve(tile, () => done = true);
                while (!done) yield return null;
                Progress.MarkVisited(tile.id);
                tileRenderers[tile.id].color = VisitedTint;
            }

            SetState(tile.type == TileType.Boss ? BoardState.Finished : BoardState.Idle);
            if (tile.type == TileType.Boss) SetMessage("Chegou ao Chefe!");
        }

        /// <summary>Resolução padrão da casa. Batalha e Chefe trocam de cena; as demais casas entram no M5.</summary>
        void DefaultResolve(BoardTile tile, Action done)
        {
            switch (tile.type)
            {
                case TileType.Battle:
                case TileType.Boss:
                    GameManager.Instance.StartBattle(tile);   // a cena Board é descarregada; ResumeAfterBattle continua
                    break;
                case TileType.Rest:
                    OpenPanel(TilePanels.Rest(hudCanvas.transform, Run, () => EndPanel(done)));
                    break;
                case TileType.Shop:
                    OpenPanel(TilePanels.Shop(hudCanvas.transform, Run, Shop.Create(Run), () => EndPanel(done)));
                    break;
                case TileType.Event:
                    OpenPanel(TilePanels.Event(hudCanvas.transform, Run, Run.NextEvent(), () => EndPanel(done)));
                    break;
                case TileType.Chest:
                    OpenPanel(TilePanels.Chest(hudCanvas.transform, Run, () => EndPanel(done)));
                    break;
                default:
                    done();
                    break;
            }
        }

        /// <summary>Painel da casa atual (loja, descanso, evento, baú); null se nenhum.</summary>
        public GameObject TilePanel { get; private set; }

        void OpenPanel(GameObject panel) { TilePanel = panel; RefreshHud(); }

        void EndPanel(Action done)
        {
            TilePanel = null;
            RefreshHud();
            done();
        }

        /// <summary>Volta da batalha vencida: mostra a recompensa, aplica a escolha e só então marca a casa como resolvida.</summary>
        IEnumerator ResumeAfterBattle()
        {
            SetState(BoardState.Resolving);
            int tileId = Run.PendingTileId;
            RefreshHud();
            if (Run.Reward != null)
            {
                int choice = -2;
                SetMessage("Escolha sua recompensa");
                pickReward = i => choice = i;
                rewardPanel = Panels.Reward(hudCanvas.transform, Run.Reward, i => pickReward?.Invoke(i));
                while (choice == -2) yield return null;
                pickReward = null;
                ClaimReward(choice);
            }
            Run.PendingTileId = -1;
            Progress.MarkVisited(tileId);
            tileRenderers[tileId].color = VisitedTint;
            SetState(BoardState.Idle);
            SetMessage("Role o dado para andar!");
        }

        Action<int> pickReward;

        /// <summary>Aplica a recompensa: moedas e a carta escolhida (índice) ou nenhuma (-1).</summary>
        public void ClaimReward(int index)
        {
            var r = Run.Reward;
            if (r == null) return;
            Run.AddCoins(r.Coins);
            if (index >= 0 && index < r.Choices.Count) Run.AddCard(r.Choices[index]);
            Run.Reward = null;
            if (rewardPanel != null) { Destroy(rewardPanel); rewardPanel = null; }
            RefreshHud();
        }

        /// <summary>Escolhe a carta de índice i no painel de recompensa (-1 pula). Mesmo caminho do clique; usado pelos testes.</summary>
        public void PickReward(int index) => pickReward?.Invoke(index);

        public static string Describe(TileType t)
        {
            switch (t)
            {
                case TileType.Battle: return "Batalha!";
                case TileType.Shop: return "Loja";
                case TileType.Rest: return "Descanso";
                case TileType.Event: return "Evento";
                case TileType.Chest: return "Baú";
                case TileType.Boss: return "O Rei Slime!";
                default: return "Início";
            }
        }

        // ---------- Sprites gerados por código ----------

        static Sprite MakeDot()
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 32f);
        }

        static Sprite MakeArrow()
        {
            // Seta para baixo, 7x5 px.
            string[] rows = { "#######", ".#####.", "..###..", "...#...", "......." };
            var tex = new Texture2D(7, 5, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < 5; y++)
                for (int x = 0; x < 7; x++)
                    tex.SetPixel(x, 4 - y, rows[y][x] == '#' ? Color.white : Color.clear);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 7, 5), new Vector2(0.5f, 0.5f), 32f);
        }
    }

    static class ListExt
    {
        public static bool Contains(this IReadOnlyList<int> list, int id)
        {
            for (int i = 0; i < list.Count; i++) if (list[i] == id) return true;
            return false;
        }
    }
}
