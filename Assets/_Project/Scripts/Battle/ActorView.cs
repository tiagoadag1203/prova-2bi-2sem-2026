using System;
using System.Collections;
using UnityEngine;

namespace XequeDeTinta.Battle
{
    /// <summary>
    /// Representação de um combatente na cena: sprite, Animator e os Animation Events
    /// (OnImpact no quadro de impacto do ataque, OnDeathEnd no fim da morte).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ActorView : MonoBehaviour
    {
        public Combatant Combatant;
        public SpriteRenderer Renderer;
        public Animator Animator;
        public float baseScale = 1f;
        public bool Dead { get; private set; }

        /// <summary>Disparado pelo Animation Event no quadro de impacto.</summary>
        public event Action Impact;

        public void Init(Combatant c, Sprite sprite, RuntimeAnimatorController controller, float scale, int sortingOrder)
        {
            Combatant = c;
            Renderer = GetComponent<SpriteRenderer>();
            Renderer.sprite = sprite;
            Renderer.sortingOrder = sortingOrder;
            baseScale = scale;
            transform.localScale = new Vector3(scale, scale, 1f);
            if (controller != null)
            {
                Animator = gameObject.GetComponent<Animator>();
                if (Animator == null) Animator = gameObject.AddComponent<Animator>();
                Animator.runtimeAnimatorController = controller;
            }
        }

        public void SetSpeed(float speed) { if (Animator != null) Animator.speed = speed; }

        // Animation Event (nome exato exigido pelo clipe).
        public void OnImpact() => Impact?.Invoke();

        // Animation Event ao fim do clipe de morte.
        public void OnDeathEnd() { if (gameObject.activeInHierarchy) StartCoroutine(Fade()); }

        /// <summary>Pisca o sprite numa cor (dano recebido).</summary>
        public void Flash(Color color, float seconds = 0.12f)
        {
            if (Dead || !gameObject.activeInHierarchy) return;
            StartCoroutine(FlashRoutine(color, seconds));
        }

        IEnumerator FlashRoutine(Color color, float seconds)
        {
            Renderer.color = color;
            float t = 0f;
            while (t < seconds) { t += Time.deltaTime; yield return null; }
            if (!Dead) Renderer.color = Color.white;
        }

        public void PlayAttack() => Trigger("attack");
        public void PlayTrigger(string name) => Trigger(name);
        public void PlayHit() { if (!Dead) Trigger("hit"); }

        public void PlayDeath()
        {
            if (Dead) return;
            Dead = true;
            if (Animator != null) Trigger("death");
            else StartCoroutine(Fade());
        }

        void Trigger(string name)
        {
            if (Animator == null || Animator.runtimeAnimatorController == null) return;
            bool has = false;
            foreach (var prm in Animator.parameters) if (prm.name == name) { has = true; break; }
            if (!has) return;
            Animator.ResetTrigger(name);
            Animator.SetTrigger(name);
        }

        IEnumerator Fade()
        {
            if (Combatant is Hero) yield break; // o herói fica caído
            float t = 0f;
            var c = Renderer.color;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                c.a = 1f - Mathf.Clamp01(t / 0.5f);
                Renderer.color = c;
                yield return null;
            }
            Renderer.enabled = false;
        }
    }
}
