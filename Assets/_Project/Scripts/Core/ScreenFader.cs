using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace XequeDeTinta.Core
{
    /// <summary>Transição entre cenas: escurece antes de trocar e clareia ao carregar. Usa tempo não escalado (funciona na pausa).</summary>
    public class ScreenFader : MonoBehaviour
    {
        static ScreenFader instance;
        Image overlay;
        bool faded;

        public static ScreenFader Get()
        {
            if (instance == null)
            {
                var go = new GameObject("ScreenFader", typeof(Canvas), typeof(CanvasScaler));
                DontDestroyOnLoad(go);
                var canvas = go.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1000;
                instance = go.AddComponent<ScreenFader>();
                var imgGo = new GameObject("Preto", typeof(RectTransform), typeof(Image));
                imgGo.transform.SetParent(go.transform, false);
                var rt = (RectTransform)imgGo.transform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
                instance.overlay = imgGo.GetComponent<Image>();
                instance.overlay.color = new Color(0.04f, 0.02f, 0.08f, 0f);
                instance.overlay.raycastTarget = false;
                SceneManager.sceneLoaded += (_, __) => { if (instance != null && instance.faded) instance.StartCoroutine(instance.Fade(1f, 0f, 0.25f)); };
            }
            return instance;
        }

        /// <summary>Escurece a tela. Depois disso, a cena seguinte clareia sozinha ao carregar.</summary>
        public IEnumerator FadeOut(float seconds = 0.22f)
        {
            faded = true;
            overlay.raycastTarget = true;
            yield return Fade(0f, 1f, seconds);
        }

        IEnumerator Fade(float from, float to, float seconds)
        {
            float t = 0f;
            var c = overlay.color;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                c.a = Mathf.Lerp(from, to, Mathf.Clamp01(t / seconds));
                overlay.color = c;
                yield return null;
            }
            c.a = to; overlay.color = c;
            if (to == 0f) { faded = false; overlay.raycastTarget = false; }
        }
    }
}
