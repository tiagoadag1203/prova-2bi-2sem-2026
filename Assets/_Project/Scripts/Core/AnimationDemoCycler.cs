using System.Collections;
using UnityEngine;

namespace XequeDeTinta
{
    /// <summary>
    /// Cena de teste do M0: repete idle, os gatilhos informados e volta ao idle, para ver tudo animando.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class AnimationDemoCycler : MonoBehaviour
    {
        public string[] triggers = { "attack", "hit", "death" };
        public float idleSeconds = 1.5f;
        public float stateSeconds = 1.2f;
        public float startDelay;

        IEnumerator Start()
        {
            var animator = GetComponent<Animator>();
            yield return new WaitForSeconds(startDelay);
            while (true)
            {
                animator.Play("idle", 0, 0f);
                yield return new WaitForSeconds(idleSeconds);
                foreach (var t in triggers)
                {
                    animator.ResetTrigger(t);
                    animator.SetTrigger(t);
                    yield return new WaitForSeconds(stateSeconds);
                }
            }
        }
    }
}
