using System.Collections.Generic;
using XequeDeTinta.Data;

namespace XequeDeTinta.Battle
{
    /// <summary>Base de Chumbo: começa toda batalha com escudo.</summary>
    public class StartShieldRelic : IBattleHook
    {
        readonly int amount;
        public StartShieldRelic(int amount) { this.amount = amount; }

        public void OnPlayerTurnStart(BattleSystem b, bool first, List<BattleEvent> events)
        {
            if (!first) return;
            b.Hero.AddShield(amount);
            events.Add(new BattleEvent(BattleEventKind.Shield, b.Hero, b.Hero, amount));
        }

        public void OnEnemyKilled(BattleSystem b, Enemy e, List<BattleEvent> events) { }
    }

    /// <summary>Pincel Afiado: começa toda batalha com Força.</summary>
    public class StartStrengthRelic : IBattleHook
    {
        readonly int amount;
        public StartStrengthRelic(int amount) { this.amount = amount; }

        public void OnPlayerTurnStart(BattleSystem b, bool first, List<BattleEvent> events)
        {
            if (!first) return;
            b.Hero.Strength += amount;
            events.Add(new BattleEvent(BattleEventKind.Strength, b.Hero, b.Hero, amount));
        }

        public void OnEnemyKilled(BattleSystem b, Enemy e, List<BattleEvent> events) { }
    }

    /// <summary>Ampulheta de Bolso: energia extra só no primeiro turno de cada batalha.</summary>
    public class FirstTurnEnergyRelic : IBattleHook
    {
        readonly int amount;
        public FirstTurnEnergyRelic(int amount) { this.amount = amount; }

        public void OnPlayerTurnStart(BattleSystem b, bool first, List<BattleEvent> events)
        {
            if (first) b.Energy += amount;
        }

        public void OnEnemyKilled(BattleSystem b, Enemy e, List<BattleEvent> events) { }
    }

    /// <summary>Tinta Seca: ao matar um inimigo, cura.</summary>
    public class HealOnKillRelic : IBattleHook
    {
        readonly int amount;
        public HealOnKillRelic(int amount) { this.amount = amount; }

        public void OnPlayerTurnStart(BattleSystem b, bool first, List<BattleEvent> events) { }

        public void OnEnemyKilled(BattleSystem b, Enemy e, List<BattleEvent> events)
        {
            if (!b.Hero.IsAlive) return;
            int healed = b.Hero.Heal(amount);
            events.Add(new BattleEvent(BattleEventKind.Heal, b.Hero, b.Hero, healed));
        }
    }

    public static class RelicHooks
    {
        /// <summary>Cria o gancho da relíquia, ou null se ela age fora da batalha (Dado Viciado age no tabuleiro).</summary>
        public static IBattleHook Create(RelicData r)
        {
            switch (r.kind)
            {
                case RelicKind.StartShield: return new StartShieldRelic(r.value);
                case RelicKind.StartStrength: return new StartStrengthRelic(r.value);
                case RelicKind.FirstTurnEnergy: return new FirstTurnEnergyRelic(r.value);
                case RelicKind.HealOnKill: return new HealOnKillRelic(r.value);
                default: return null;
            }
        }
    }
}
