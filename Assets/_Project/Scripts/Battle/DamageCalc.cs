using System;
using XequeDeTinta.Data;

namespace XequeDeTinta.Battle
{
    /// <summary>Números de regra copiados do GameConfig; mantêm a lógica independente de ScriptableObject.</summary>
    public class BattleRules
    {
        public int EnergyPerTurn = 3;
        public int HandSize = 5;
        public int BurnDamage = 2;
        public float EffectiveMultiplier = 1.5f;
        public float WeakMultiplier = 0.75f;

        public static BattleRules From(GameConfig c) => new BattleRules
        {
            EnergyPerTurn = c.energyPerTurn,
            HandSize = c.handSize,
            BurnDamage = c.burnDamage,
            EffectiveMultiplier = c.effectiveMultiplier,
            WeakMultiplier = c.weakMultiplier,
        };
    }

    public static class ElementRules
    {
        /// <summary>Fogo vence Planta, Planta vence Água, Água vence Fogo. Neutro não tem vantagem.</summary>
        public static bool Beats(Element attacker, Element target) =>
            (attacker == Element.Fire && target == Element.Plant) ||
            (attacker == Element.Plant && target == Element.Water) ||
            (attacker == Element.Water && target == Element.Fire);
    }

    public static class DamageCalc
    {
        public readonly struct Result
        {
            public readonly int amount;
            public readonly bool effective;
            public Result(int amount, bool effective) { this.amount = amount; this.effective = effective; }
        }

        /// <summary>base + Força → ×1,5 se eficaz → ×0,75 se atacante Fraco → arredonda para baixo.</summary>
        public static Result Calculate(int baseDamage, Element attackElement, int strength, bool attackerWeak,
                                       Element targetElement, BattleRules rules)
        {
            double v = baseDamage + strength;
            bool effective = ElementRules.Beats(attackElement, targetElement);
            if (effective) v *= rules.EffectiveMultiplier;
            if (attackerWeak) v *= rules.WeakMultiplier;
            int amount = (int)Math.Floor(v + 1e-6);
            return new Result(Math.Max(0, amount), effective);
        }
    }
}
