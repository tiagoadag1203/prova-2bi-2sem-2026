namespace XequeDeTinta.Data
{
    public enum Element { Neutral, Fire, Plant, Water }

    public enum CardTarget { Self, SingleEnemy, AllEnemies }

    /// <summary>Tipo de efeito de carta; mapeado para classes pequenas em XequeDeTinta.Battle (M3).</summary>
    public enum EffectKind { DealDamage, GainShield, ApplyBurn, ApplyWeak, Heal, Stun }

    public enum IntentType { Attack, Defend, Buff, Debuff, Summon }

    public enum EnemyBehaviorKind
    {
        /// <summary>Repete sempre a ação 0.</summary>
        Simple,
        /// <summary>Sorteia entre as ações; a ação de escudo (índice 1) nunca sai duas vezes seguidas.</summary>
        RandomNoRepeatDefend,
        /// <summary>Ação 0 normalmente; a ação 1 a cada N turnos.</summary>
        EveryNthSpecial,
        /// <summary>Percorre as ações em ordem, em ciclo.</summary>
        Cycle,
    }

    public enum TileType { Start, Battle, Shop, Rest, Event, Chest, Boss }

    public enum RelicKind { RerollDice, StartShield, HealOnKill, StartStrength, FirstTurnEnergy }

    public enum EventOutcomeKind
    {
        Nothing, LoseHp, Heal, GainCoins, GainRelic, GainRandomUpgradedCard, RemoveCard,
        /// <summary>Rola 1d6: 1-2 perde value de vida; 3-4 ganha value2 moedas; 5-6 ganha 1 relíquia.</summary>
        DiceGamble,
    }
}
