using System.Collections.Generic;
using XequeDeTinta.Data;

namespace XequeDeTinta.Battle
{
    /// <summary>Uma carta do deck da partida (a mesma CardData pode aparecer várias vezes, cada cópia com seu estado de melhoria).</summary>
    public class CardInstance
    {
        public CardData Data;
        public bool Upgraded;

        public CardInstance(CardData data, bool upgraded = false) { Data = data; Upgraded = upgraded; }

        public int Cost => Data.cost;
        public Element Element => Data.element;
        public bool Exhaust => Data.exhaust;
        public string Name => Upgraded ? Data.displayName + "+" : Data.displayName;
        public string Description => Data.GetDescription(Upgraded);
        public IReadOnlyList<EffectSpec> Effects => Data.GetEffects(Upgraded);
        public bool CanUpgrade => !Upgraded;
    }
}
