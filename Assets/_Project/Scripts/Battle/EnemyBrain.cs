using XequeDeTinta.Core;
using XequeDeTinta.Data;

namespace XequeDeTinta.Battle
{
    /// <summary>Escolhe a intenção do próximo turno de um inimigo, conforme o comportamento do EnemyData.</summary>
    public static class EnemyBrain
    {
        public static void ChooseIntent(Enemy e, IRng rng)
        {
            var d = e.Data;
            int count = d.actions.Count;
            int idx = 0;
            switch (d.behavior)
            {
                case EnemyBehaviorKind.Simple:
                    idx = 0;
                    break;
                case EnemyBehaviorKind.RandomNoRepeatDefend:
                    idx = rng.Range(0, count);
                    if (idx == 1 && e.LastIndex == 1) idx = 0; // nunca escudo duas vezes seguidas
                    break;
                case EnemyBehaviorKind.EveryNthSpecial:
                    int turn = e.TurnsActed + 1;
                    idx = d.specialEveryN > 0 && turn % d.specialEveryN == 0 ? 1 : 0;
                    break;
                case EnemyBehaviorKind.Cycle:
                    idx = e.TurnsActed % count;
                    break;
            }
            e.IntentIndex = idx;
            e.Intent = d.actions[idx];
        }
    }
}
