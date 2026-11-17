using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace XequeDeTinta.UI
{
    /// <summary>Pausa (Esc): Continuar e Voltar ao título. Congela o tempo enquanto aberta.</summary>
    public class PauseMenu : MonoBehaviour
    {
        static int suppressFrame = -1;
        /// <summary>Quem consumiu o Esc neste quadro (ex.: cancelar o alvo da carta) chama isto para não pausar também.</summary>
        public static void Suppress() => suppressFrame = Time.frameCount;

        GameObject panel;
        Action onBackToTitle;
        public bool IsPaused => panel != null && panel.activeSelf;

        public static PauseMenu Attach(Transform canvas, Action onBackToTitle)
        {
            var go = new GameObject("PauseMenu");
            go.transform.SetParent(canvas, false);
            var pm = go.AddComponent<PauseMenu>();
            pm.onBackToTitle = onBackToTitle;
            pm.Build(canvas);
            return pm;
        }

        void Build(Transform canvas)
        {
            var overlay = Panels.Overlay(canvas, "PainelPausa");
            overlay.SetAsLastSibling();
            panel = overlay.gameObject;
            var box = UIFactory.CreatePanel("Painel", overlay, new Vector2(0.5f, 0.5f), new Vector2(240, 130), Vector2.zero);
            var t = UIFactory.CreateText("Titulo", box, "Pausado", 12, Palette.Gold);
            t.rectTransform.anchorMin = new Vector2(0, 1); t.rectTransform.anchorMax = new Vector2(1, 1);
            t.rectTransform.pivot = new Vector2(0.5f, 1); t.rectTransform.sizeDelta = new Vector2(0, 16);
            t.rectTransform.anchoredPosition = new Vector2(0, -10);
            UIFactory.CreateButton("Continuar", box, "Continuar", new Vector2(0.5f, 0.5f), new Vector2(150, 22), new Vector2(0, 2), Resume, 10);
            UIFactory.CreateButton("VoltarTitulo", box, "Voltar ao título", new Vector2(0.5f, 0.5f), new Vector2(150, 22), new Vector2(0, -28), BackToTitle, 10);
            panel.SetActive(false);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
            if (suppressFrame == Time.frameCount) return;
            if (IsPaused) Resume(); else Pause();
        }

        public void Pause()
        {
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            panel.SetActive(false);
            Time.timeScale = 1f;
        }

        void BackToTitle()
        {
            Time.timeScale = 1f;
            onBackToTitle?.Invoke();
        }

        void OnDestroy() { if (Time.timeScale == 0f) Time.timeScale = 1f; }
    }
}
