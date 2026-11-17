using System;
using System.Collections.Generic;
using UnityEngine;

namespace XequeDeTinta.Data
{
    /// <summary>Uma ação que o inimigo pode escolher como intenção.</summary>
    [Serializable]
    public class EnemyAction
    {
        public string name;
        public IntentType intent;
        public int damage;
        public int shield;
        /// <summary>Turnos de Queimadura aplicados ao herói.</summary>
        public int burnTurns;
        /// <summary>Vida curada no aliado mais ferido (ou em si mesmo).</summary>
        public int heal;
        /// <summary>Quantos aliados invoca.</summary>
        public int summonCount;
        /// <summary>Força ganha (Buff).</summary>
        public int strength;
    }

    [CreateAssetMenu(menuName = "Xeque de Tinta/Inimigo", fileName = "Enemy_Novo")]
    public class EnemyData : ScriptableObject
    {
        public string id;
        public string displayName;
        public Element element;
        [Min(1)] public int maxHp = 10;
        public EnemyBehaviorKind behavior;
        public List<EnemyAction> actions = new List<EnemyAction>();
        /// <summary>Para EveryNthSpecial: a ação 1 sai a cada N turnos.</summary>
        public int specialEveryN;
        /// <summary>Abaixo desta fração de vida, ganha enrageStrength de Força uma vez (0 = desligado).</summary>
        [Range(0f, 1f)] public float enrageBelowHpFraction;
        public int enrageStrength;
        /// <summary>Inimigo invocado pela ação de Invocar e o máximo de vivos ao mesmo tempo.</summary>
        public EnemyData summonedEnemy;
        public int summonMaxAlive;
        public Sprite sprite;
        public float spriteScale = 1f;
        public RuntimeAnimatorController animator;
        public bool isBoss;
    }
}
