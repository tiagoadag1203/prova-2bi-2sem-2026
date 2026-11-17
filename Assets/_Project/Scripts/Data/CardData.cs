using System.Collections.Generic;
using UnityEngine;

namespace XequeDeTinta.Data
{
    [CreateAssetMenu(menuName = "Xeque de Tinta/Carta", fileName = "Card_Nova")]
    public class CardData : ScriptableObject
    {
        public string id;
        public string displayName;
        [Min(0)] public int cost = 1;
        public Element element;
        public CardTarget target = CardTarget.SingleEnemy;
        public List<EffectSpec> effects = new List<EffectSpec>();
        public List<EffectSpec> upgradedEffects = new List<EffectSpec>();
        public bool exhaust;
        /// <summary>Entra no pool de recompensas, loja e eventos.</summary>
        public bool inRewardPool;
        [TextArea] public string description;
        [TextArea] public string upgradedDescription;
        public Sprite art;

        public IReadOnlyList<EffectSpec> GetEffects(bool upgraded) => upgraded ? upgradedEffects : effects;
        public string GetDescription(bool upgraded) => upgraded ? upgradedDescription : description;
    }
}
