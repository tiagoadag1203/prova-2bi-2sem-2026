using System.Collections.Generic;
using XequeDeTinta.Data;

namespace XequeDeTinta.Battle
{
    public class EffectContext
    {
        public BattleRules Rules;
        /// <summary>Elemento do ataque (o da carta).</summary>
        public Element Element;
    }

    /// <summary>Efeito de carta: classe pequena e combinável. O CardData só descreve (EffectSpec); a fábrica cria as classes.</summary>
    public interface ICardEffect
    {
        /// <summary>Se verdadeiro, age no usuário (escudo, cura); senão, no alvo escolhido.</summary>
        bool TargetsUser { get; }
        void Apply(EffectContext ctx, Combatant user, Combatant target, List<BattleEvent> events);
    }

    public class DealDamage : ICardEffect
    {
        readonly int value, hits;
        public DealDamage(int value, int hits = 1) { this.value = value; this.hits = System.Math.Max(1, hits); }
        public bool TargetsUser => false;

        public void Apply(EffectContext ctx, Combatant user, Combatant target, List<BattleEvent> events)
        {
            for (int i = 0; i < hits && target.IsAlive; i++)
            {
                var r = DamageCalc.Calculate(value, ctx.Element, user.Strength, user.Weak > 0, target.Element, ctx.Rules);
                int lost = target.TakeDamage(r.amount, out int absorbed);
                events.Add(new BattleEvent(BattleEventKind.Damage, user, target, lost, r.effective, absorbed));
            }
        }
    }

    public class GainShield : ICardEffect
    {
        readonly int value;
        public GainShield(int value) { this.value = value; }
        public bool TargetsUser => true;

        public void Apply(EffectContext ctx, Combatant user, Combatant target, List<BattleEvent> events)
        {
            user.AddShield(value);
            events.Add(new BattleEvent(BattleEventKind.Shield, user, user, value));
        }
    }

    public class ApplyBurn : ICardEffect
    {
        readonly int turns;
        public ApplyBurn(int turns) { this.turns = turns; }
        public bool TargetsUser => false;

        public void Apply(EffectContext ctx, Combatant user, Combatant target, List<BattleEvent> events)
        {
            target.Burn += turns; // reaplicar soma turnos
            events.Add(new BattleEvent(BattleEventKind.Burn, user, target, turns));
        }
    }

    public class ApplyWeak : ICardEffect
    {
        readonly int turns;
        public ApplyWeak(int turns) { this.turns = turns; }
        public bool TargetsUser => false;

        public void Apply(EffectContext ctx, Combatant user, Combatant target, List<BattleEvent> events)
        {
            target.Weak += turns;
            events.Add(new BattleEvent(BattleEventKind.Weak, user, target, turns));
        }
    }

    public class Heal : ICardEffect
    {
        readonly int value;
        public Heal(int value) { this.value = value; }
        public bool TargetsUser => true;

        public void Apply(EffectContext ctx, Combatant user, Combatant target, List<BattleEvent> events)
        {
            int healed = user.Heal(value);
            events.Add(new BattleEvent(BattleEventKind.Heal, user, user, healed));
        }
    }

    public class Stun : ICardEffect
    {
        public bool TargetsUser => false;

        public void Apply(EffectContext ctx, Combatant user, Combatant target, List<BattleEvent> events)
        {
            if (!target.IsAlive) return;
            target.Stunned = true;
            events.Add(new BattleEvent(BattleEventKind.Stun, user, target));
        }
    }

    public static class EffectFactory
    {
        public static ICardEffect Create(EffectSpec s)
        {
            switch (s.kind)
            {
                case EffectKind.DealDamage: return new DealDamage(s.value, s.hits);
                case EffectKind.GainShield: return new GainShield(s.value);
                case EffectKind.ApplyBurn: return new ApplyBurn(s.value);
                case EffectKind.ApplyWeak: return new ApplyWeak(s.value);
                case EffectKind.Heal: return new Heal(s.value);
                case EffectKind.Stun: return new Stun();
                default: throw new System.ArgumentOutOfRangeException(nameof(s), s.kind.ToString());
            }
        }
    }
}
