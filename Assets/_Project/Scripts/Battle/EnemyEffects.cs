using System.Collections.Generic;
using XequeDeTinta.Data;

namespace XequeDeTinta.Battle
{
    /// <summary>Efeito de uma ação de inimigo. Pequenas classes combináveis, como as das cartas.</summary>
    public interface IEnemyEffect
    {
        void Apply(BattleSystem battle, Enemy self, List<BattleEvent> events);
    }

    public class EnemyAttack : IEnemyEffect
    {
        readonly int damage;
        public EnemyAttack(int damage) { this.damage = damage; }

        public void Apply(BattleSystem battle, Enemy self, List<BattleEvent> events)
        {
            var hero = battle.Hero;
            var r = DamageCalc.Calculate(damage, self.Element, self.Strength, self.Weak > 0, hero.Element, battle.Rules);
            int lost = hero.TakeDamage(r.amount, out int absorbed);
            events.Add(new BattleEvent(BattleEventKind.Damage, self, hero, lost, r.effective, absorbed));
        }
    }

    public class EnemyShield : IEnemyEffect
    {
        readonly int amount;
        public EnemyShield(int amount) { this.amount = amount; }

        public void Apply(BattleSystem battle, Enemy self, List<BattleEvent> events)
        {
            self.AddShield(amount);
            events.Add(new BattleEvent(BattleEventKind.Shield, self, self, amount));
        }
    }

    public class EnemyBurnHero : IEnemyEffect
    {
        readonly int turns;
        public EnemyBurnHero(int turns) { this.turns = turns; }

        public void Apply(BattleSystem battle, Enemy self, List<BattleEvent> events)
        {
            battle.Hero.Burn += turns;
            events.Add(new BattleEvent(BattleEventKind.Burn, self, battle.Hero, turns));
        }
    }

    /// <summary>Cura o aliado mais ferido (maior vida faltando); se ninguém está ferido, não faz nada.</summary>
    public class EnemyHealAlly : IEnemyEffect
    {
        readonly int amount;
        public EnemyHealAlly(int amount) { this.amount = amount; }

        public void Apply(BattleSystem battle, Enemy self, List<BattleEvent> events)
        {
            Enemy best = null; int bestMissing = 0;
            foreach (var e in battle.Enemies)
            {
                if (!e.IsAlive) continue;
                int missing = e.MaxHp - e.Hp;
                if (missing > bestMissing) { best = e; bestMissing = missing; }
            }
            if (best == null) return;
            int healed = best.Heal(amount);
            events.Add(new BattleEvent(BattleEventKind.Heal, self, best, healed));
        }
    }

    public class EnemyGainStrength : IEnemyEffect
    {
        readonly int amount;
        public EnemyGainStrength(int amount) { this.amount = amount; }

        public void Apply(BattleSystem battle, Enemy self, List<BattleEvent> events)
        {
            self.Strength += amount;
            events.Add(new BattleEvent(BattleEventKind.Strength, self, self, amount));
        }
    }

    /// <summary>Invoca o inimigo definido no EnemyData, respeitando o máximo de vivos desse tipo.</summary>
    public class EnemySummon : IEnemyEffect
    {
        readonly int count;
        public EnemySummon(int count) { this.count = count; }

        public void Apply(BattleSystem battle, Enemy self, List<BattleEvent> events)
        {
            var type = self.Data.summonedEnemy;
            if (type == null) return;
            int alive = 0;
            foreach (var e in battle.Enemies) if (e.IsAlive && e.Data == type) alive++;
            int room = self.Data.summonMaxAlive - alive;
            for (int i = 0; i < count && i < room; i++)
            {
                var spawned = battle.SpawnEnemy(type);
                events.Add(new BattleEvent(BattleEventKind.Summon, self, spawned));
            }
        }
    }

    public static class EnemyEffectFactory
    {
        /// <summary>Converte uma ação do EnemyData na lista de efeitos que ela executa, na ordem do GDD (escudo, depois dano).</summary>
        public static List<IEnemyEffect> Build(EnemyAction a)
        {
            var list = new List<IEnemyEffect>();
            if (a.shield > 0) list.Add(new EnemyShield(a.shield));
            if (a.strength > 0) list.Add(new EnemyGainStrength(a.strength));
            if (a.summonCount > 0) list.Add(new EnemySummon(a.summonCount));
            if (a.burnTurns > 0) list.Add(new EnemyBurnHero(a.burnTurns));
            if (a.damage > 0) list.Add(new EnemyAttack(a.damage));
            if (a.heal > 0) list.Add(new EnemyHealAlly(a.heal));
            return list;
        }
    }
}
