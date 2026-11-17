using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XequeDeTinta.Core;
using XequeDeTinta.Data;

namespace XequeDeTinta.UI
{
    /// <summary>Tela de título: logo, Jogar, Sair e volume. Peão e slimes animados de enfeite.</summary>
    public class TitleController : MonoBehaviour
    {
        public GameConfig config;
        [System.Serializable]
        public class Decor
        {
            public Sprite sprite;
            public RuntimeAnimatorController animator;
            public Vector2 position;
            public float scale = 3f;
        }
        public Decor[] decor;
        /// <summary>Logo em sprite (lote 2). Se nulo, usa texto.</summary>
        public Sprite logo;

        void Start()
        {
            GameManager.Ensure(config);
            Time.timeScale = 1f;
            var art = UIArt.Get();
            if (logo == null && art != null) logo = art.logo;
            if (art != null && art.backgroundBoard != null)
            {
                var bg = new GameObject("Fundo", typeof(SpriteRenderer));
                bg.transform.SetParent(transform, false);
                var bsr = bg.GetComponent<SpriteRenderer>(); bsr.sprite = art.backgroundBoard; bsr.sortingOrder = -20;
                var dim = new GameObject("Escurecer", typeof(SpriteRenderer));
                dim.transform.SetParent(transform, false);
                var dsr = dim.GetComponent<SpriteRenderer>();
                var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                tex.SetPixel(0, 0, Color.white); tex.Apply();
                dsr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                dsr.color = new Color(0.1f, 0.05f, 0.18f, 0.5f); dsr.sortingOrder = -19;
                dim.transform.localScale = new Vector3(40f, 24f, 1f);
            }

            foreach (var d in decor)
            {
                var go = new GameObject("Enfeite", typeof(SpriteRenderer), typeof(Animator));
                go.transform.SetParent(transform, false);
                go.transform.position = d.position;
                go.transform.localScale = new Vector3(d.scale, d.scale, 1f);
                var sr = go.GetComponent<SpriteRenderer>();
                sr.sprite = d.sprite; sr.sortingOrder = 1;
                go.GetComponent<Animator>().runtimeAnimatorController = d.animator;
            }

            var canvas = UIFactory.CreateCanvas("TituloUI", 10, transform);
            UIFactory.EnsureEventSystem();

            if (logo != null)
            {
                var img = UIFactory.Anchored("Logo", canvas.transform, new Vector2(0.5f, 1), new Vector2(256, 96), new Vector2(0, -20));
                var im = img.gameObject.AddComponent<Image>(); im.sprite = logo; im.preserveAspect = true;
            }
            else
            {
                var t = UIFactory.CreateText("Titulo", canvas.transform, "XEQUE DE TINTA", 24, Palette.Gold);
                SetTop(t.rectTransform, 40, 30);
                var sub = UIFactory.CreateText("Sub", canvas.transform, "Roguelite de tabuleiro e cartas", 8, Palette.Cream);
                SetTop(sub.rectTransform, 78, 12);
            }

            UIFactory.CreateButton("Jogar", canvas.transform, "Jogar", new Vector2(0.5f, 0.5f), new Vector2(120, 22), new Vector2(0, 30), Play, 10);
            UIFactory.CreateButton("Sair", canvas.transform, "Sair", new Vector2(0.5f, 0.5f), new Vector2(120, 22), new Vector2(0, 2), Quit, 10);

            var label = UIFactory.CreateText("VolumeTxt", canvas.transform, "Volume", 8, Palette.Cream);
            var lr = label.rectTransform;
            lr.anchorMin = lr.anchorMax = new Vector2(0.5f, 0.5f); lr.sizeDelta = new Vector2(100, 10); lr.anchoredPosition = new Vector2(0, -22);
            BuildVolumeSlider(canvas.transform, new Vector2(0, -36));

            var credit = UIFactory.CreateText("Creditos", canvas.transform, "Márcio Dias de Lima Filho e Tiago Augusto Dal'Acqua Gonçalves", 8, new Color(1, 1, 1, 0.5f));
            var cr = credit.rectTransform;
            cr.anchorMin = new Vector2(0, 0); cr.anchorMax = new Vector2(1, 0); cr.pivot = new Vector2(0.5f, 0);
            cr.sizeDelta = new Vector2(0, 10); cr.anchoredPosition = new Vector2(0, 4);
        }

        static void SetTop(RectTransform r, float y, float h)
        {
            r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1); r.pivot = new Vector2(0.5f, 1);
            r.sizeDelta = new Vector2(0, h); r.anchoredPosition = new Vector2(0, -y);
        }

        void BuildVolumeSlider(Transform parent, Vector2 pos)
        {
            var root = UIFactory.Anchored("Volume", parent, new Vector2(0.5f, 0.5f), new Vector2(140, 12), pos);
            var bg = root.gameObject.AddComponent<Image>();
            bg.color = Palette.PurpleLight;

            var fillArea = UIFactory.Rect("AreaPreenchimento", root, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));
            var fill = UIFactory.Rect("Preenchimento", fillArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fillImg = fill.gameObject.AddComponent<Image>(); fillImg.color = Palette.Gold;

            var handleArea = UIFactory.Rect("AreaAlca", root, Vector2.zero, Vector2.one, new Vector2(4, 0), new Vector2(-4, 0));
            var handle = UIFactory.Rect("Alca", handleArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            handle.sizeDelta = new Vector2(8, 0);
            var handleImg = handle.gameObject.AddComponent<Image>(); handleImg.color = Palette.Cream;

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f; slider.maxValue = 1f;
            slider.value = AudioListener.volume;
            slider.onValueChanged.AddListener(v => AudioListener.volume = v);
        }

        public void Play() => GameManager.Ensure(config).StartRun();

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
