namespace XequeDeTinta.Battle
{
    public enum BattleEventKind
    {
        Damage, Shield, Heal, Burn, Weak, Stun, Strength, Summon, Death, BurnTick, StunSkip, Enrage,
    }

    /// <summary>O que aconteceu ao resolver uma jogada; a apresentação usa isto para números flutuantes e animações.</summary>
    public readonly struct BattleEvent
    {
        public readonly BattleEventKind kind;
        public readonly Combatant source;
        public readonly Combatant target;
        public readonly int amount;
        public readonly bool effective;
        /// <summary>Dano absorvido pelo escudo.</summary>
        public readonly int blocked;

        public BattleEvent(BattleEventKind kind, Combatant source, Combatant target, int amount = 0, bool effective = false, int blocked = 0)
        {
            this.kind = kind; this.source = source; this.target = target;
            this.amount = amount; this.effective = effective; this.blocked = blocked;
        }

        public override string ToString() => $"{kind} {target?.Name} {amount}{(effective ? " eficaz" : "")}";
    }
}
