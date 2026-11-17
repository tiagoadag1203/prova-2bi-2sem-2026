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
    public enum BoardState { Idle, Rolling, Moving, Choosing, Resolving, Finished }

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

        // HUD (alimentado pelo RunState a partir do M4)
        public int hp = 50, maxHp = 50, coins;

        BoardGraph graph;
        BoardMover mover;
        readonly Dictionary<int, SpriteRenderer> tileRenderers = new Dictionary<int, SpriteRenderer>();
        readonly List<Transform> markers = new List<Transform>();
        Transform pawn;
        Animator pawnAnim;
        SpriteRenderer diceRenderer;
        TextMeshProUGUI statusText, messageText;
        Button rollButton;
        Camera cam;
        int chosen = -1;
        Sprite dotSprite, arrowSprite;

        static readonly Color VisitedTint = new Color(0.42f, 0.42f, 0.48f, 1f);

        void Start()
        {
            cam = Camera.main;
            graph = new BoardGraph(config.board.tiles);
            if (Progress == null) Progress = new BoardProgress(config.board.startTileId);
            mover = new BoardMover(graph, Progress.CurrentTile);
            dotSprite = MakeDot();
            arrowSprite = MakeArrow();
            BuildTiles();
            BuildPawn();
            BuildDice();
            BuildHud();
            SetState(BoardState.Idle);
            SetMessage("Role o dado para andar!");
        }

        // ---------- Construção ----------

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
            UIFactory.EnsureEventSystem();

            statusText = UIFactory.CreateText("Status", canvas.transform, "", 8, Palette.Cream, TextAlignmentOptions.TopLeft, false);
            statusText.rectTransform.anchorMin = new Vector2(0, 1); statusText.rectTransform.anchorMax = new Vector2(0, 1);
            statusText.rectTransform.pivot = new Vector2(0, 1);
            statusText.rectTransform.sizeDelta = new Vector2(300, 14);
            statusText.rectTransform.anchoredPosition = new Vector2(8, -8);

            messageText = UIFactory.CreateText("Mensagem", canvas.transform, "", 8, Palette.Gold, TextAlignmentOptions.Top);
            messageText.rectTransform.anchorMin = new Vector2(0.5f, 1); messageText.rectTransform.anchorMax = new Vector2(0.5f, 1);
            messageText.rectTransform.pivot = new Vector2(0.5f, 1);
            messageText.rectTransform.sizeDelta = new Vector2(420, 24);
            messageText.rectTransform.anchoredPosition = new Vector2(0, -30);

            rollButton = UIFactory.CreateButton("Rolar", canvas.transform, "Rolar dado (Espaço)",
                new Vector2(0.5f, 0), new Vector2(200, 20), new Vector2(0, 8), RollDice);
            UIFactory.CreateButton("VerDeck", canvas.transform, "Ver deck",
                new Vector2(1, 1), new Vector2(70, 18), new Vector2(-8, -8), () => DeckRequested?.Invoke());
            RefreshHud();
        }

        public void SetHud(int hp, int maxHp, int coins)
        {
            this.hp = hp; this.maxHp = maxHp; this.coins = coins;
            RefreshHud();
        }

        void RefreshHud()
        {
            if (statusText != null) statusText.text = $"Vida {hp}/{maxHp}   Moedas {coins}";
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
            if (State == BoardState.Idle && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                RollDice();

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

        IEnumerator RollRoutine()
        {
            SetState(BoardState.Rolling);
            SetMessage("Rolando...");
            float t = 0;
            int frames = 0;
            while (t < Dur(rollSeconds))
            {
                diceRenderer.sprite = diceFaces[UnityEngine.Random.Range(0, diceFaces.Length)];
                diceRenderer.transform.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-12f, 12f));
                float step = Dur(0.06f);
                yield return new WaitForSeconds(step);
                t += step; frames++;
            }
            diceRenderer.transform.localRotation = Quaternion.identity;
            LastRoll = DiceRoller.Roll(Rng, config.diceSides);
            diceRenderer.sprite = diceFaces[Mathf.Clamp(LastRoll, 1, diceFaces.Length) - 1];
            SetMessage($"Tirou {LastRoll}!");
            RollFinished?.Invoke(LastRoll);
            yield return new WaitForSeconds(Dur(0.25f));

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
                else done = true;
                while (!done) yield return null;
                Progress.MarkVisited(tile.id);
                tileRenderers[tile.id].color = VisitedTint;
            }

            SetState(tile.type == TileType.Boss ? BoardState.Finished : BoardState.Idle);
            if (tile.type == TileType.Boss) SetMessage("Chegou ao Chefe!");
        }

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
