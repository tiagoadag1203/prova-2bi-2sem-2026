using XequeDeTinta.Data;

namespace XequeDeTinta.Battle
{
    /// <summary>Quem entra em combate: herói ou inimigo. Lógica pura.</summary>
    public class Combatant
    {
        public string Name;
        public Element Element;
        public int MaxHp;
        public int Hp;
        public int Shield;
        /// <summary>Turnos restantes de Queimadura (2 de vida no início do turno do dono).</summary>
        public int Burn;
        /// <summary>Turnos restantes de Fraco (-25% de dano).</summary>
        public int Weak;
        /// <summary>Dano extra em cada ataque até o fim da batalha.</summary>
        public int Strength;
        /// <summary>Pula a próxima ação.</summary>
        public bool Stunned;
        /// <summary>Já anunciada como morta (evita evento duplicado).</summary>
        public bool DeathAnnounced;

        public bool IsAlive => Hp > 0;

        /// <summary>Dano que passa pelo escudo. Devolve a vida perdida; 'absorbed' é o que o escudo segurou.</summary>
        public int TakeDamage(int amount, out int absorbed)
        {
            if (amount < 0) amount = 0;
            absorbed = System.Math.Min(Shield, amount);
            Shield -= absorbed;
            int lost = amount - absorbed;
            Hp = System.Math.Max(0, Hp - lost);
            return lost;
        }

        /// <summary>Perda direta de vida (Queimadura), ignora o escudo.</summary>
        public int LoseHp(int amount)
        {
            int lost = System.Math.Min(Hp, System.Math.Max(0, amount));
            Hp -= lost;
            return lost;
        }

        /// <summary>Cura até a vida máxima. Devolve quanto curou de fato.</summary>
        public int Heal(int amount)
        {
            int healed = System.Math.Min(MaxHp - Hp, System.Math.Max(0, amount));
            Hp += healed;
            return healed;
        }

        public void AddShield(int amount) { if (amount > 0) Shield += amount; }
    }

    public class Hero : Combatant { }

    public class Enemy : Combatant
    {
        public EnemyData Data;
        public EnemyAction Intent;
        public int IntentIndex;
        /// <summary>Quantas ações já executou (alimenta o ciclo do chefe e o "a cada N turnos").</summary>
        public int TurnsActed;
        public int LastIndex = -1;
        public bool Enraged;
    }
}
