using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XequeDeTinta.Data;

namespace XequeDeTinta.UI
{
    /// <summary>Faixa de relíquias do HUD: um quadrado por relíquia (ícone ou iniciais) com tooltip ao passar o mouse.</summary>
    public class RelicBar : MonoBehaviour
    {
        RectTransform root, tip;
        TextMeshProUGUI tipText;
        readonly List<GameObject> slots = new List<GameObject>();

        public static RelicBar Create(Transform canvas)
        {
            var go = new GameObject("RelicBar", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            var bar = go.AddComponent<RelicBar>();
            bar.root = (RectTransform)go.transform;
            bar.root.anchorMin = bar.root.anchorMax = bar.root.pivot = new Vector2(0, 1);
            bar.root.sizeDelta = new Vector2(220, 22);
            bar.root.anchoredPosition = new Vector2(8, -22);

            bar.tip = UIFactory.Anchored("Tooltip", canvas, new Vector2(0, 1), new Vector2(220, 40), new Vector2(8, -48));
            var bg = bar.tip.gameObject.AddComponent<Image>(); bg.color = new Color(0.1f, 0.05f, 0.15f, 0.95f); bg.raycastTarget = false;
            bar.tipText = UIFactory.CreateText("Texto", bar.tip, "", 8, Palette.Cream, TextAlignmentOptions.TopLeft);
            bar.tipText.rectTransform.offsetMin = new Vector2(4, 3); bar.tipText.rectTransform.offsetMax = new Vector2(-4, -3);
            bar.tip.gameObject.SetActive(false);
            return bar;
        }

        public void Rebuild(IReadOnlyList<RelicData> relics)
        {
            foreach (var s in slots) Destroy(s);
            slots.Clear();
            for (int i = 0; i < relics.Count; i++)
            {
                var r = relics[i];
                var slot = UIFactory.Anchored("Relica_" + r.id, root, new Vector2(0, 1), new Vector2(20, 20), new Vector2(i * 24f, 0));
                var img = slot.gameObject.AddComponent<Image>();
                img.color = r.icon != null ? Color.white : Palette.Gold;
                if (r.icon != null) { img.sprite = r.icon; img.preserveAspect = true; }
                if (r.icon == null)
                {
                    var t = UIFactory.CreateText("Sigla", slot, Initials(r.displayName), 8, Palette.Purple);
                }
                var hover = slot.gameObject.AddComponent<HoverTip>();
                hover.Setup(() => Show($"{r.displayName}\n{r.description}"), Hide);
                slots.Add(slot.gameObject);
            }
            Hide();
        }

        void Show(string text) { tipText.text = text; tip.gameObject.SetActive(true); }
        void Hide() { tip.gameObject.SetActive(false); }

        static string Initials(string name)
        {
            var parts = name.Split(' ');
            string s = "";
            foreach (var p in parts) if (p.Length > 2) s += char.ToUpper(p[0]);
            return s.Length == 0 ? name.Substring(0, 1) : s;
        }
    }

    public class HoverTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        System.Action onEnter, onExit;
        public void Setup(System.Action enter, System.Action exit) { onEnter = enter; onExit = exit; }
        public void OnPointerEnter(PointerEventData e) => onEnter?.Invoke();
        public void OnPointerExit(PointerEventData e) => onExit?.Invoke();
    }
}
