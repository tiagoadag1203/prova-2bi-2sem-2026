using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XequeDeTinta.Core;
using XequeDeTinta.Data;

namespace XequeDeTinta.UI
{
    /// <summary>Tela final: Vitória (com créditos curtos) ou Derrota (com "Tentar de novo").</summary>
    public class EndController : MonoBehaviour
    {
        public GameConfig config;
        public Sprite background, pawnSprite;
        public RuntimeAnimatorController pawnAnimator;
        public Sprite logo;

        public bool Won { get; private set; }

        void Start()
        {
            var gm = GameManager.Ensure(config);
            Won = gm.LastRunWon ?? false;
            Time.timeScale = 1f;

            if (background != null)
            {
                var bg = new GameObject("Fundo", typeof(SpriteRenderer));
                bg.transform.SetParent(transform, false);
                var sr = bg.GetComponent<SpriteRenderer>(); sr.sprite = background; sr.sortingOrder = -20;
                var dim = new GameObject("Escurecer", typeof(SpriteRenderer));
                dim.transform.SetParent(transform, false);
                var ds = dim.GetComponent<SpriteRenderer>(); ds.sprite = MakeBox(); ds.color = new Color(0.1f, 0.05f, 0.18f, Won ? 0.45f : 0.7f);
                ds.sortingOrder = -19; dim.transform.localScale = new Vector3(40, 24, 1);
            }

            var pawn = new GameObject("Peao", typeof(SpriteRenderer), typeof(Animator));
            pawn.transform.SetParent(transform, false);
            pawn.transform.position = new Vector3(0, -3.4f, 0);
            pawn.transform.localScale = new Vector3(3, 3, 1);
            var psr = pawn.GetComponent<SpriteRenderer>(); psr.sprite = pawnSprite; psr.sortingOrder = 2;
            var anim = pawn.GetComponent<Animator>(); anim.runtimeAnimatorController = pawnAnimator;
            // Vitória: o peão comemora pulando; derrota: caído.
            if (pawnAnimator != null) anim.Play(Won ? "hop" : "death", 0, 0f);
            if (Won) pawn.AddComponent<LoopTrigger>().Init(anim, "hop", 0.9f);

            var canvas = UIFactory.CreateCanvas("FimUI", 10, transform);
            UIFactory.EnsureEventSystem();

            var title = UIFactory.CreateText("Titulo", canvas.transform, Won ? "VITÓRIA!" : "DERROTA", 24, Won ? Palette.Gold : Palette.Red);
            Top(title.rectTransform, 22, 30);

            string story = Won
                ? "O Rei Slime se desfaz em tinta e a tinta seca.\nO tabuleiro está limpo, e o peão comemora!"
                : "O peão tombou sobre o tabuleiro.\nMas toda peça pode voltar ao jogo.";
            var body = UIFactory.CreateText("Texto", canvas.transform, story, 8, Palette.Cream);
            Top(body.rectTransform, 62, 30);

            if (Won)
            {
                var credits = UIFactory.CreateText("Creditos", canvas.transform,
                    "Xeque de Tinta\n\nDesign e programação\nMárcio Dias de Lima Filho\nTiago Augusto Dal'Acqua Gonçalves\n\nArte gerada no PixelLab · Fonte Press Start 2P (OFL)\nObrigado por jogar!",
                    8, new Color(1, 1, 1, 0.85f));
                Top(credits.rectTransform, 108, 90);
                UIFactory.CreateButton("VoltarTitulo", canvas.transform, "Voltar ao título", new Vector2(0.5f, 0), new Vector2(160, 22), new Vector2(0, 100), ToTitle, 10);
            }
            else
            {
                UIFactory.CreateButton("TentarDeNovo", canvas.transform, "Tentar de novo", new Vector2(0.5f, 0.5f), new Vector2(160, 22), new Vector2(0, 8), TryAgain, 10);
                UIFactory.CreateButton("VoltarTitulo", canvas.transform, "Voltar ao título", new Vector2(0.5f, 0.5f), new Vector2(160, 22), new Vector2(0, -22), ToTitle, 10);
            }
        }

        static void Top(RectTransform r, float y, float h)
        {
            r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1); r.pivot = new Vector2(0.5f, 1);
            r.offsetMin = new Vector2(20, 0); r.offsetMax = new Vector2(-20, 0);
            r.sizeDelta = new Vector2(r.sizeDelta.x, h); r.anchoredPosition = new Vector2(0, -y);
        }

        static Sprite MakeBox()
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        public void TryAgain() => GameManager.Ensure(config).StartRun();
        public void ToTitle() => GameManager.Ensure(config).BackToTitle();

        class LoopTrigger : MonoBehaviour
        {
            Animator anim; string trigger; float every, t;
            public void Init(Animator a, string tr, float seconds) { anim = a; trigger = tr; every = seconds; }
            void Update()
            {
                t += Time.deltaTime;
                if (t >= every && anim != null) { t = 0; anim.ResetTrigger(trigger); anim.SetTrigger(trigger); }
            }
        }
    }
}
