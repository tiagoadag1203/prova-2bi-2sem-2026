using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using XequeDeTinta.Core;
using XequeDeTinta.Data;
using XequeDeTinta.UI;

namespace XequeDeTinta.Battle
{
    /// <summary>
    /// Ponte entre o BattleSystem (regras) e a cena: atores, HUD, mão de cartas, alvo, números flutuantes.
    /// O dano só é aplicado quando o Animation Event de impacto dispara.
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        [Header("Dados")]
        public GameConfig config;
        [Tooltip("Só para teste solto da cena: índice em config.battleEncounters; -1 usa o chefe.")]
        public int debugEncounterIndex = 4;

        [Header("Arte (ligada pelo construtor da cena)")]
        public Sprite heroSprite;
        public RuntimeAnimatorController heroAnimator;
        public Sprite[] intentSprites;   // indexado por IntentType
        static UIArt Art => UIArt.Get();

        [Header("Ritmo")]
        public float speed = 1f;         // os testes aceleram isto

        public BattleSystem Sys { get; private set; }
        public bool Busy { get; private set; }
        public BattleResult Result { get; private set; }
        public bool ResultShown { get; private set; }
        public event Action<BattleResult> Finished;

        // Cena
        static readonly Vector2 HeroFeet = new Vector2(-4.7f, -0.7f);
        const float HandBand = 100f;   // faixa inferior (px do quadro 640x360) reservada para a mão
        ActorView heroView;
        readonly Dictionary<Combatant, ActorView> views = new Dictionary<Combatant, ActorView>();
        readonly Dictionary<Combatant, ActorHud> huds = new Dictionary<Combatant, ActorHud>();

        // HUD
        Canvas canvas;
        RectTransform frame;     // quadro 640x360 onde a interface ancora
        RectTransform handRoot, floaters;
        TextMeshProUGUI messageText, energyText, drawText, discardText;
        Button endTurnButton;
        readonly List<CardView> handViews = new List<CardView>();
        CardView pending;               // carta esperando o alvo
        CardView hovered;
        Transform targetArrow;
        Sprite arrowSprite;
        GameObject resultPanel;

        class ActorHud
        {
            public RectTransform root;
            public Image fill;
            public TextMeshProUGUI hp, status;
            public RectTransform statusRow;
            public string statusKey = "";
            public readonly List<GameObject> statusItems = new List<GameObject>();
            public Image intentIcon;
            public TextMeshProUGUI intentText;
            public GameObject intentRoot;
        }

        // ---------- Início ----------

        void Start()
        {
            if (BattleLauncher.SpeedOverride > 0f) speed = BattleLauncher.SpeedOverride;
            var setup = BattleLauncher.Setup ?? DefaultSetup();
            Sys = new BattleSystem(setup);
            arrowSprite = MakeArrow();
            canvas = UIFactory.CreateCanvas("BattleHUD", 10, transform);
            frame = UIFactory.CreateFrame(canvas);
            UIFactory.EnsureEventSystem();
            BuildStage();
            BuildHud();
            PauseMenu.Attach(canvas.transform, () => GameManager.Instance?.BackToTitle());
            Sys.Begin();
            SetSpeed();
            StartCoroutine(OpeningRoutine());
        }

        BattleSetup DefaultSetup()
        {
            var enc = debugEncounterIndex >= 0 && debugEncounterIndex < config.battleEncounters.Count
                ? config.battleEncounters[debugEncounterIndex] : config.bossEncounter;
            var deck = new List<CardInstance>();
            foreach (var c in config.starterDeck) deck.Add(new CardInstance(c));
            return new BattleSetup
            {
                HeroHp = config.heroMaxHp, HeroMaxHp = config.heroMaxHp, Deck = deck, Encounter = enc,
                Rules = BattleRules.From(config), Rng = new SeededRng(),
            };
        }

        IEnumerator OpeningRoutine()
        {
            Busy = true;
            RefreshAll();
            SetMessage("Seu turno");
            yield return Wait(0.3f);
            yield return Present(Sys.TurnStartEvents);
            Busy = false;
            RefreshAll();
        }

        void SetSpeed()
        {
            foreach (var v in views.Values) v.SetSpeed(speed);
        }

        float Dur(float s) => s / Mathf.Max(0.01f, speed);
        WaitForSeconds Wait(float s) => new WaitForSeconds(Dur(s));

        // ---------- Construção ----------

        static Vector2 ToCanvas(Vector2 world) => new Vector2((world.x + 10f) * 32f, (world.y + 5.625f) * 32f);

        bool bossLayout;
        int initialEnemies = 1;

        // Posições X (mundo) dos inimigos, com espaçamento mínimo de ~2,4 unidades (76 px) para nada se sobrepor.
        static readonly float[] BossSlots = { 2.4f, 6.2f, 8.6f };               // Rei Slime + 2 Mirins
        static readonly float[][] Slots =
        {
            new[] { 4.8f },
            new[] { 3.2f, 6.6f },
            new[] { 1.6f, 4.9f, 8.2f },
        };

        Vector2 EnemyFeet(int index)
        {
            float x = bossLayout
                ? BossSlots[Mathf.Min(index, BossSlots.Length - 1)]
                : Slots[Mathf.Clamp(initialEnemies, 1, 3) - 1][Mathf.Min(index, Mathf.Clamp(initialEnemies, 1, 3) - 1)];
            return new Vector2(x, -0.7f);
        }

        /// <summary>Altura do topo do sprite (para intenções, números e seta), contando a escala.</summary>
        static float TopOf(ActorView v) => v.transform.position.y + (v.Renderer != null ? v.Renderer.bounds.size.y : 2f);

        void BuildStage()
        {
            if (Art != null && Art.backgroundBattle != null)
            {
                var bg = new GameObject("Fundo", typeof(SpriteRenderer));
                bg.transform.SetParent(transform, false);
                var sr = bg.GetComponent<SpriteRenderer>();
                sr.sprite = Art.backgroundBattle; sr.sortingOrder = -20;
                // Escurece um pouco para os personagens e a interface se destacarem.
                var dim = new GameObject("Escurecer", typeof(SpriteRenderer));
                dim.transform.SetParent(transform, false);
                var ds = dim.GetComponent<SpriteRenderer>();
                ds.sprite = MakeBox(Color.white); ds.color = new Color(0.08f, 0.04f, 0.15f, 0.58f); ds.sortingOrder = -19;
                dim.transform.localScale = new Vector3(40f, 24f, 1f);
            }
            else
            {
                var ground = new GameObject("Chao", typeof(SpriteRenderer));
                ground.transform.SetParent(transform, false);
                var sr = ground.GetComponent<SpriteRenderer>();
                sr.sprite = MakeBox(Color.white);
                sr.color = new Color(0.36f, 0.25f, 0.5f, 1f);
                sr.sortingOrder = -10;
                ground.transform.position = new Vector3(0f, -3.3f, 0f);
                ground.transform.localScale = new Vector3(80f, 4.65f, 1f);
            }
            bossLayout = Sys.Enemies.Count > 0 && Sys.Enemies[0].Data.isBoss;
            initialEnemies = Sys.Enemies.Count;

            heroView = CreateActor("Heroi", Sys.Hero, heroSprite, heroAnimator, 3f, HeroFeet, 2);
            AddHud(Sys.Hero, false);
            for (int i = 0; i < Sys.Enemies.Count; i++) AddEnemy(Sys.Enemies[i], i);
        }

        ActorView CreateActor(string name, Combatant c, Sprite sprite, RuntimeAnimatorController anim, float scale, Vector2 feet, int order)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            go.transform.position = feet;
            var view = go.AddComponent<ActorView>();
            view.Init(c, sprite != null ? sprite : Placeholder(c.Name), anim, scale, order);
            views[c] = view;
            return view;
        }

        void AddEnemy(Enemy e, int index)
        {
            var d = e.Data;
            var view = CreateActor("Inimigo_" + e.Name + "_" + index, e, d.sprite, d.animator, 2f * d.spriteScale, EnemyFeet(index), 1);
            var col = view.gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.8f, 1.8f); col.offset = new Vector2(0f, 1f);
            view.SetSpeed(speed);
            AddHud(e, true);
        }

        void AddHud(Combatant c, bool enemy)
        {
            var feet = views[c].transform.position;
            var h = new ActorHud();
            // Bloco logo abaixo do personagem, centrado nele: barra, número, escudo/status.
            h.root = UIFactory.Anchored("Hud_" + c.Name, frame, Vector2.zero, new Vector2(76, 40), Vector2.zero);
            h.root.pivot = new Vector2(0.5f, 1f);
            h.root.anchoredPosition = ToCanvas(new Vector2(feet.x, feet.y - 0.1f));

            var barBg = UIFactory.Anchored("BarraFundo", h.root, new Vector2(0.5f, 1), new Vector2(58, 8), Vector2.zero);
            barBg.pivot = new Vector2(0.5f, 1);
            var bg = barBg.gameObject.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.05f, 0.15f, 1f); bg.raycastTarget = false;
            var fillRt = UIFactory.Rect("Barra", barBg, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -1));
            h.fill = fillRt.gameObject.AddComponent<Image>();
            h.fill.color = enemy ? Palette.Red : Palette.Green; h.fill.raycastTarget = false;
            h.fill.type = Image.Type.Filled; h.fill.fillMethod = Image.FillMethod.Horizontal;

            h.hp = UIFactory.CreateText("Vida", h.root, "", 8, Palette.Cream, TextAlignmentOptions.Top, false);
            var hr = h.hp.rectTransform;
            hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 1); hr.pivot = new Vector2(0.5f, 1);
            hr.sizeDelta = new Vector2(76, 10); hr.anchoredPosition = new Vector2(0, -9);

            h.statusRow = UIFactory.Anchored("LinhaStatus", h.root, new Vector2(0.5f, 1), new Vector2(76, 16), new Vector2(0, -21));
            h.statusRow.pivot = new Vector2(0.5f, 1);
            h.status = UIFactory.CreateText("Status", h.root, "", 8, Palette.Cream, TextAlignmentOptions.Top, false);
            var sr = h.status.rectTransform;
            sr.anchorMin = sr.anchorMax = new Vector2(0.5f, 1); sr.pivot = new Vector2(0.5f, 1);
            sr.sizeDelta = new Vector2(76, 16); sr.anchoredPosition = new Vector2(0, -21);

            if (enemy)
            {
                // Intenção logo acima da cabeça deste inimigo.
                var headWorld = new Vector2(feet.x, TopOf(views[c]) + 0.1f);
                var ir = UIFactory.Anchored("Intencao", frame, Vector2.zero, new Vector2(56, 32), Vector2.zero);
                ir.pivot = new Vector2(0.5f, 0f);
                ir.anchoredPosition = ToCanvas(headWorld);
                h.intentRoot = ir.gameObject;
                var icon = UIFactory.Rect("Icone", ir, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(2, -16), new Vector2(34, 16));
                h.intentIcon = icon.gameObject.AddComponent<Image>();
                h.intentIcon.raycastTarget = false; h.intentIcon.preserveAspect = true;
                h.intentText = UIFactory.CreateText("Numero", ir, "", 12, Palette.Gold, TextAlignmentOptions.Left, false);
                h.intentText.rectTransform.offsetMin = new Vector2(36, 0);
            }
            huds[c] = h;
        }

        /// <summary>Pilha de cartas (Compra/Descarte): caixa com rótulo e número grande.</summary>
        TextMeshProUGUI BuildPile(string name, string label, Vector2 anchor, Vector2 pos)
        {
            var box = UIFactory.Anchored("Pilha" + name, frame, anchor, new Vector2(74, 38), pos);
            var border = box.gameObject.AddComponent<Image>(); border.color = Palette.Gold; border.raycastTarget = false;
            var inner = UIFactory.Rect("Fundo", box, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));
            var bg = inner.gameObject.AddComponent<Image>(); bg.color = Palette.Purple; bg.raycastTarget = false;
            var lt = UIFactory.CreateText("Rotulo", inner, label, 8, Palette.Cream, TextAlignmentOptions.Top, false);
            lt.rectTransform.offsetMin = new Vector2(0, 14); lt.rectTransform.offsetMax = new Vector2(0, -2);
            var n = UIFactory.CreateText("Numero", inner, "0", 12, Palette.Gold, TextAlignmentOptions.Center, false);
            n.rectTransform.offsetMin = new Vector2(0, 1); n.rectTransform.offsetMax = new Vector2(0, -14);
            return n;
        }

        void BuildHud()
        {
            messageText = UIFactory.CreateText("Mensagem", frame, "", 8, Palette.Gold, TextAlignmentOptions.Top);
            var mr = messageText.rectTransform;
            mr.anchorMin = mr.anchorMax = new Vector2(0.5f, 1); mr.pivot = new Vector2(0.5f, 1);
            mr.sizeDelta = new Vector2(420, 14); mr.anchoredPosition = new Vector2(0, -8);

            // Faixa da mão: cartas sempre dentro dela; o campo fica acima.
            handRoot = UIFactory.Anchored("Mao", frame, new Vector2(0.5f, 0), new Vector2(640, HandBand), Vector2.zero);
            floaters = UIFactory.Rect("Flutuantes", frame, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Zona esquerda da mão: energia e pilha de compra.
            var orb = UIFactory.Anchored("Energia", frame, new Vector2(0, 0), new Vector2(32, 32), new Vector2(12, 56));
            var orbImg = orb.gameObject.AddComponent<Image>();
            orbImg.raycastTarget = false;
            bool hasOrb = Art != null && Art.energyOrb != null;
            if (hasOrb) { orbImg.sprite = Art.energyOrb; orbImg.preserveAspect = true; } else orbImg.color = Palette.Gold;
            energyText = UIFactory.CreateText("Valor", frame, "3/3", 12, Palette.Gold, TextAlignmentOptions.Left, false);
            var er = energyText.rectTransform;
            er.anchorMin = er.anchorMax = Vector2.zero; er.pivot = new Vector2(0, 0.5f);
            er.sizeDelta = new Vector2(60, 16); er.anchoredPosition = new Vector2(48, 72);
            drawText = BuildPile("Compra", "Compra", new Vector2(0, 0), new Vector2(12, 8));

            // Zona direita: pilha de descarte e botão de encerrar o turno.
            discardText = BuildPile("Descarte", "Descarte", new Vector2(1, 0), new Vector2(-12, 8));
            endTurnButton = UIFactory.CreateButton("EncerrarTurno", frame, "Encerrar turno",
                new Vector2(1, 0), new Vector2(124, 24), new Vector2(-8, 56), EndTurn);
        }

        // ---------- Atualização do HUD ----------

        void RefreshAll()
        {
            RefreshHud();
            RefreshHand();
        }

        void SetMessage(string t) { if (messageText != null) messageText.text = t; }

        static string StatusLine(Combatant c)
        {
            var sb = new System.Text.StringBuilder();
            if (c.Shield > 0) sb.Append("<color=#7FB8FF>Esc ").Append(c.Shield).Append("</color> ");
            if (c.Burn > 0) sb.Append("<color=#FF9A4A>Queim ").Append(c.Burn).Append("</color> ");
            if (c.Weak > 0) sb.Append("<color=#C79BFF>Fraco ").Append(c.Weak).Append("</color> ");
            if (c.Strength > 0) sb.Append("<color=#7CE07C>Forç ").Append(c.Strength).Append("</color> ");
            if (c.Stunned) sb.Append("<color=#F2C14E>Atord</color>");
            return sb.ToString();
        }

        /// <summary>Linha de status com ícone + número; sem os sprites, cai para o texto colorido.</summary>
        void RefreshStatus(Combatant c, ActorHud h)
        {
            var statusSprites = Art != null ? Art.statusIcons : null;
            bool icons = statusSprites != null && statusSprites.Length >= 5 && statusSprites[0] != null;
            if (!icons) { h.status.text = StatusLine(c); return; }
            h.status.text = "";
            string key = $"{c.Shield}|{c.Burn}|{c.Weak}|{c.Strength}|{c.Stunned}";
            if (key == h.statusKey) return;
            h.statusKey = key;
            foreach (var g in h.statusItems) Destroy(g);
            h.statusItems.Clear();

            var active = new List<(int icon, int value)>();
            if (c.Shield > 0) active.Add((0, c.Shield));
            if (c.Burn > 0) active.Add((1, c.Burn));
            if (c.Weak > 0) active.Add((2, c.Weak));
            if (c.Strength > 0) active.Add((3, c.Strength));
            if (c.Stunned) active.Add((4, 0));

            const float step = 28f;
            float start = -(active.Count - 1) * step / 2f;
            for (int i = 0; i < active.Count; i++)
            {
                var item = UIFactory.Anchored("Status" + i, h.statusRow, new Vector2(0.5f, 0.5f), new Vector2(28, 16), new Vector2(start + i * step, 0));
                var img = UIFactory.Rect("Icone", item, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, -8), new Vector2(16, 8)).gameObject.AddComponent<Image>();
                img.sprite = statusSprites[active[i].icon]; img.raycastTarget = false;
                if (active[i].value > 0)
                {
                    var t = UIFactory.CreateText("N", item, active[i].value.ToString(), 8, Palette.Cream, TextAlignmentOptions.Left, false);
                    t.rectTransform.offsetMin = new Vector2(17, 0);
                }
                h.statusItems.Add(item.gameObject);
            }
        }

        void RefreshHud()
        {
            foreach (var kv in huds)
            {
                var c = kv.Key; var h = kv.Value;
                bool show = c.IsAlive || c is Hero;
                h.root.gameObject.SetActive(show && (views[c].Renderer == null || views[c].Renderer.enabled));
                h.fill.fillAmount = c.MaxHp > 0 ? Mathf.Clamp01(c.Hp / (float)c.MaxHp) : 0f;
                h.hp.text = c.Hp + "/" + c.MaxHp;
                RefreshStatus(c, h);
                if (h.intentRoot != null)
                {
                    var e = (Enemy)c;
                    bool alive = e.IsAlive && Sys.Phase != BattlePhase.Victory;
                    h.intentRoot.SetActive(alive && e.Intent != null);
                    if (alive && e.Intent != null)
                    {
                        var idx = (int)e.Intent.intent;
                        h.intentIcon.sprite = intentSprites != null && idx < intentSprites.Length ? intentSprites[idx] : null;
                        h.intentIcon.enabled = h.intentIcon.sprite != null;
                        int dmg = Sys.PreviewDamage(e);
                        h.intentText.text = dmg > 0 ? dmg.ToString() : "";
                    }
                }
            }
            energyText.text = Sys.Energy + "/" + Sys.Rules.EnergyPerTurn;
            drawText.text = Sys.Deck.DrawPile.Count.ToString();
            discardText.text = Sys.Deck.DiscardPile.Count.ToString();
            endTurnButton.interactable = !Busy && Sys.Phase == BattlePhase.PlayerTurn && pending == null;
        }

        void RefreshHand()
        {
            foreach (var v in handViews) if (v != null) Destroy(v.gameObject);
            handViews.Clear();
            hovered = null;
            var hand = Sys.Deck.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                var cv = CardView.Create(handRoot, hand[i], Vector2.zero);
                cv.SetPlayable(Sys.CanPlay(i));
                cv.Clicked += OnCardClicked;
                cv.HoverChanged += OnCardHover;
                handViews.Add(cv);
            }
            LayoutHand();
        }

        const float CardScale = 0.76f;

        /// <summary>Cartas menores em leque dentro da faixa inferior; a carta sob o mouse sobe e aumenta.</summary>
        void LayoutHand()
        {
            int n = handViews.Count;
            if (n == 0) return;
            float cardW = CardView.Width * CardScale;
            float step = n > 1 ? Mathf.Min(cardW - 2f, (340f - cardW) / (n - 1)) : 0f;
            float mid = (n - 1) / 2f;
            for (int i = 0; i < n; i++)
            {
                var v = handViews[i];
                float off = i - mid;
                if (v == hovered)
                {
                    v.Rect.anchoredPosition = new Vector2(off * step, 30f);
                    v.Rect.localScale = new Vector3(1.2f, 1.2f, 1f);
                    v.Rect.localRotation = Quaternion.identity;
                }
                else if (v == pending)
                {
                    v.Rect.anchoredPosition = new Vector2(off * step, 12f);
                    v.Rect.localScale = new Vector3(0.95f, 0.95f, 1f);
                    v.Rect.localRotation = Quaternion.identity;
                }
                else
                {
                    v.Rect.anchoredPosition = new Vector2(off * step, 9f - off * off * 0.8f);
                    v.Rect.localScale = new Vector3(CardScale, CardScale, 1f);
                    v.Rect.localRotation = Quaternion.Euler(0f, 0f, -off * 3f);
                }
            }
            if (hovered != null) hovered.Rect.SetAsLastSibling();
            else if (pending != null) pending.Rect.SetAsLastSibling();
        }

        void OnCardHover(CardView v, bool on)
        {
            if (Busy) return;
            hovered = on ? v : (hovered == v ? null : hovered);
            LayoutHand();
        }

        // ---------- Entrada ----------

        void Update()
        {
            if (Sys == null) return;
            var kb = Keyboard.current; var mouse = Mouse.current;

            if (pending != null)
            {
                ShowTargetArrow();
                bool cancel = (kb != null && kb.escapeKey.wasPressedThisFrame) ||
                              (mouse != null && mouse.rightButton.wasPressedThisFrame);
                if (cancel) { PauseMenu.Suppress(); CancelTargeting(); return; }
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    var e = EnemyUnderMouse();
                    if (e != null) ConfirmTarget(e);
                }
                return;
            }
            HideTargetArrow();

            if (kb != null && kb.spaceKey.wasPressedThisFrame) EndTurn();
        }

        Enemy EnemyUnderMouse()
        {
            var cam = Camera.main;
            if (cam == null || Mouse.current == null) return null;
            Vector2 w = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            foreach (var h in Physics2D.OverlapPointAll(w))
            {
                var v = h.GetComponent<ActorView>();
                if (v != null && v.Combatant is Enemy e && e.IsAlive) return e;
            }
            return null;
        }

        void ShowTargetArrow()
        {
            var e = EnemyUnderMouse();
            if (e == null) { HideTargetArrow(); return; }
            if (targetArrow == null)
            {
                var go = new GameObject("SetaAlvo", typeof(SpriteRenderer));
                go.transform.SetParent(transform, false);
                go.transform.localScale = new Vector3(2, 2, 1);
                var sr = go.GetComponent<SpriteRenderer>();
                sr.sprite = arrowSprite; sr.color = Palette.Gold; sr.sortingOrder = 8;
                targetArrow = go.transform;
            }
            var pos = views[e].transform.position;
            targetArrow.position = new Vector3(pos.x, TopOf(views[e]) + 0.6f + Mathf.Sin(Time.time * 8f) * 0.06f, 0f);
            targetArrow.gameObject.SetActive(true);
        }

        void HideTargetArrow() { if (targetArrow != null) targetArrow.gameObject.SetActive(false); }

        // ---------- Jogadas (públicas para os testes) ----------

        void OnCardClicked(CardView v)
        {
            if (Busy || pending != null && v != pending)
            {
                if (pending != null) CancelTargeting();
                return;
            }
            int idx = handViews.IndexOf(v);
            if (idx < 0) return;
            if (!Sys.CanPlay(idx)) { SetMessage("Energia insuficiente!"); return; }

            if (v.Card.Data.target == CardTarget.SingleEnemy)
            {
                if (Sys.AliveCount == 1)
                {
                    int only = Sys.Enemies.FindIndex(e => e.IsAlive);
                    PlayCardAt(idx, only);
                }
                else
                {
                    pending = v;
                    SetMessage("Escolha o alvo (clique no inimigo; Esc cancela)");
                    LayoutHand(); RefreshHud();
                }
            }
            else PlayCardAt(idx, -1);
        }

        void ConfirmTarget(Enemy e)
        {
            int cardIdx = handViews.IndexOf(pending);
            pending = null;
            SetMessage("Seu turno");
            if (cardIdx >= 0) PlayCardAt(cardIdx, Sys.Enemies.IndexOf(e));
        }

        void CancelTargeting()
        {
            pending = null;
            SetMessage("Seu turno");
            LayoutHand(); RefreshHud();
        }

        public void PlayCardAt(int handIndex, int targetIndex)
        {
            if (Busy || Sys.IsOver || !Sys.CanPlay(handIndex)) return;
            StartCoroutine(PlayCardRoutine(handIndex, targetIndex));
        }

        public void EndTurn()
        {
            if (Busy || Sys.Phase != BattlePhase.PlayerTurn) return;
            pending = null;
            StartCoroutine(EnemyTurnRoutine());
        }

        IEnumerator PlayCardRoutine(int handIndex, int targetIndex)
        {
            Busy = true;
            var card = Sys.Deck.Hand[handIndex];
            var play = Sys.PlayCard(handIndex, targetIndex);
            AudioManager.Play(Sfx.Card);
            RefreshAll();

            if (HasHostileEffect(card)) yield return WaitImpact(heroView);
            else yield return Wait(0.25f);

            var events = play.Resolve();
            yield return Present(events);

            Busy = false;
            RefreshAll();
            if (Sys.IsOver) yield return FinishRoutine();
        }

        static bool HasHostileEffect(CardInstance c)
        {
            if (c.Data.target == CardTarget.Self) return false;
            foreach (var e in c.Effects)
                if (e.kind == EffectKind.DealDamage || e.kind == EffectKind.ApplyBurn || e.kind == EffectKind.ApplyWeak || e.kind == EffectKind.Stun)
                    return true;
            return false;
        }

        IEnumerator EnemyTurnRoutine()
        {
            Busy = true;
            SetMessage("Turno dos inimigos");
            Sys.EndPlayerTurn();
            RefreshAll();
            yield return Wait(0.4f);

            BattleSystem.EnemyPlay play;
            while ((play = Sys.NextEnemyAction()) != null)
            {
                yield return Present(play.PreEvents);
                if (!play.Skipped)
                {
                    var view = views[play.Enemy];
                    var a = play.Action;
                    if (a.intent == IntentType.Summon) yield return WaitImpact(view, "summon");
                    else if (a.damage > 0 || a.intent == IntentType.Debuff)
                        yield return WaitImpact(view);
                    else yield return Wait(0.3f);
                }
                yield return Present(play.Resolve());
                yield return Wait(0.2f);
                if (Sys.IsOver) break;
            }

            if (!Sys.IsOver)
            {
                yield return Present(Sys.TurnStartEvents);   // Queimadura do herói
                SetMessage("Seu turno");
            }
            Busy = false;
            RefreshAll();
            if (Sys.IsOver) yield return FinishRoutine();
        }

        /// <summary>Toca o ataque e espera o Animation Event de impacto (com tempo-limite de segurança).</summary>
        IEnumerator WaitImpact(ActorView v, string trigger = "attack")
        {
            bool hit = false;
            Action h = () => hit = true;
            v.Impact += h;
            v.PlayTrigger(trigger);
            float t = 0f, limit = Dur(1.2f);
            while (!hit && t < limit) { t += Time.deltaTime; yield return null; }
            v.Impact -= h;
            yield return Wait(0.12f);
        }

        // ---------- Apresentação dos eventos ----------

        IEnumerator Present(IReadOnlyList<BattleEvent> events)
        {
            foreach (var e in events)
            {
                switch (e.kind)
                {
                    case BattleEventKind.Damage:
                        if (e.amount > 0)
                        {
                            views[e.target].PlayHit();
                            views[e.target].Flash(e.target is Hero ? new Color(1f, 0.35f, 0.35f) : Color.white);
                            AudioManager.Play(e.target is Hero ? Sfx.Hurt : e.effective ? Sfx.HitEffective : Sfx.Hit);
                            if (e.target is Hero) Shake(Mathf.Clamp(1 + e.amount / 5, 1, 4));
                            else if (e.effective) Shake(1);
                            Float(e.target, "-" + e.amount, e.effective ? Palette.Gold : Palette.Red);
                            if (e.effective) Float(e.target, "Eficaz!", Palette.Gold, 14f);
                        }
                        else if (e.blocked > 0) { Float(e.target, "Bloqueado", new Color(0.5f, 0.72f, 1f)); AudioManager.Play(Sfx.Shield); }
                        break;
                    case BattleEventKind.BurnTick:
                        if (e.amount > 0) { views[e.target].PlayHit(); AudioManager.Play(Sfx.Burn); Float(e.target, "-" + e.amount, new Color(1f, 0.6f, 0.29f)); }
                        break;
                    case BattleEventKind.Shield: AudioManager.Play(Sfx.Shield); Float(e.target, "+" + e.amount + " Esc", new Color(0.5f, 0.72f, 1f)); break;
                    case BattleEventKind.Heal: if (e.amount > 0) { AudioManager.Play(Sfx.Heal); Float(e.target, "+" + e.amount, Palette.Green); } break;
                    case BattleEventKind.Burn: AudioManager.Play(Sfx.Burn); Float(e.target, "Queimadura", new Color(1f, 0.6f, 0.29f)); break;
                    case BattleEventKind.Weak: Float(e.target, "Fraco", new Color(0.78f, 0.6f, 1f)); break;
                    case BattleEventKind.Stun: Float(e.target, "Atordoado", Palette.Gold); break;
                    case BattleEventKind.StunSkip: Float(e.target, "Atordoado!", Palette.Gold); break;
                    case BattleEventKind.Strength:
                    case BattleEventKind.Enrage: Float(e.target, "Força +" + e.amount, Palette.Green); break;
                    case BattleEventKind.Summon: AddEnemy((Enemy)e.target, Sys.Enemies.IndexOf((Enemy)e.target)); Float(e.target, "Invocado!", Palette.Cream); AudioManager.Play(Sfx.Summon); break;
                    case BattleEventKind.Death: views[e.target].PlayDeath(); AudioManager.Play(e.target is Hero ? Sfx.Lose : Sfx.Die); if (!(e.target is Hero)) Shake(1); break;
                }
                RefreshHud();
                yield return Wait(e.kind == BattleEventKind.Damage || e.kind == BattleEventKind.Death ? 0.22f : 0.12f);
            }
        }

        // ---------- Tremor de tela leve (em passos de 1 pixel, para não borrar o pixel art) ----------

        Coroutine shaking;

        void Shake(int pixels)
        {
            var cam = Camera.main;
            if (cam == null) return;
            if (shaking != null) StopCoroutine(shaking);
            shaking = StartCoroutine(ShakeRoutine(cam.transform, pixels));
        }

        IEnumerator ShakeRoutine(Transform cam, int pixels)
        {
            var origin = new Vector3(0f, 0f, cam.position.z);
            float d = Dur(0.18f), t = 0f;
            while (t < d)
            {
                t += Time.deltaTime;
                int amp = Mathf.CeilToInt(pixels * (1f - Mathf.Clamp01(t / d)));
                cam.position = origin + new Vector3(UnityEngine.Random.Range(-amp, amp + 1), UnityEngine.Random.Range(-amp, amp + 1), 0f) / 32f;
                yield return null;
            }
            cam.position = origin;
            shaking = null;
        }

        int floatCount;

        void Float(Combatant c, string text, Color color, float extraY = 0f)
        {
            var v = views[c];
            var head = new Vector2(v.transform.position.x, Mathf.Min(TopOf(v), v.transform.position.y + 3f) - 0.3f);
            var t = UIFactory.CreateText("Num", floaters, text, 12, color, TextAlignmentOptions.Center, false);
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(120, 16);
            float jitter = ((floatCount++ % 3) - 1) * 8f;
            rt.anchoredPosition = ToCanvas(head) + new Vector2(jitter, extraY);
            StartCoroutine(FloatRoutine(t));
        }

        IEnumerator FloatRoutine(TextMeshProUGUI t)
        {
            float d = Dur(0.9f), x = 0f;
            var rt = t.rectTransform; var start = rt.anchoredPosition;
            while (x < d && t != null)
            {
                x += Time.deltaTime;
                float k = Mathf.Clamp01(x / d);
                rt.anchoredPosition = start + new Vector2(0f, 22f * k);
                t.alpha = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
                yield return null;
            }
            if (t != null) Destroy(t.gameObject);
        }

        // ---------- Fim da batalha ----------

        IEnumerator FinishRoutine()
        {
            if (ResultShown) yield break;
            Busy = true;
            yield return Wait(0.6f);
            int kills = 0; foreach (var e in Sys.Enemies) if (!e.IsAlive) kills++;
            AudioManager.Play(Sys.Phase == BattlePhase.Victory ? Sfx.Win : Sfx.Lose);
            Result = new BattleResult { Won = Sys.Phase == BattlePhase.Victory, HeroHp = Sys.Hero.Hp, HeroMaxHp = Sys.Hero.MaxHp, Kills = kills };
            ResultShown = true;
            ShowResultPanel();
        }

        void ShowResultPanel()
        {
            bool won = Result.Won;
            var panel = UIFactory.CreatePanel("Resultado", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(220, 90), new Vector2(0, 20));
            resultPanel = panel.parent.gameObject;
            var title = UIFactory.CreateText("Titulo", panel, won ? "Vitória!" : "Derrota...", 16, won ? Palette.Gold : Palette.Red);
            title.rectTransform.offsetMin = new Vector2(4, 40); title.rectTransform.offsetMax = new Vector2(-4, -6);
            UIFactory.CreateButton("Continuar", panel, BattleLauncher.OnFinished != null ? "Continuar" : "Jogar de novo",
                new Vector2(0.5f, 0), new Vector2(120, 20), new Vector2(0, 12), Continue);
        }

        /// <summary>Entrega o resultado a quem lançou a batalha (GameManager, M4) ou reinicia a cena de teste.</summary>
        public void Continue()
        {
            if (Result == null) return;
            Finished?.Invoke(Result);
            var cb = BattleLauncher.OnFinished;
            if (cb != null) { BattleLauncher.OnFinished = null; cb(Result); }
            else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // ---------- Sprites gerados por código ----------

        static Sprite MakeBox(Color c)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, c); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        /// <summary>Quadrado colorido no lugar de arte que ainda não existe (ex.: Rei Slime antes do lote 2).</summary>
        static Sprite Placeholder(string label)
        {
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    tex.SetPixel(x, y, (x == 0 || y == 0 || x == 31 || y == 31) ? Color.black : new Color(0.8f, 0.3f, 0.8f));
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0f), 32f);
        }

        static Sprite MakeArrow()
        {
            string[] rows = { "#######", ".#####.", "..###..", "...#...", "......." };
            var tex = new Texture2D(7, 5, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < 5; y++)
                for (int x = 0; x < 7; x++)
                    tex.SetPixel(x, 4 - y, rows[y][x] == '#' ? Color.white : Color.clear);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 7, 5), new Vector2(0.5f, 0.5f), 32f);
        }
    }
}
