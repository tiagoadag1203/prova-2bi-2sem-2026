using System;

namespace XequeDeTinta.Battle
{
    public class BattleResult
    {
        public bool Won;
        public int HeroHp;
        public int HeroMaxHp;
        public int Kills;
    }

    /// <summary>
    /// Passa a batalha a ser jogada para a cena Battle (e o resultado de volta). O GameManager (M4) preenche
    /// Setup e OnFinished antes de carregar a cena; sem isso a cena monta uma batalha de teste.
    /// </summary>
    public static class BattleLauncher
    {
        public static BattleSetup Setup;
        public static Action<BattleResult> OnFinished;
        /// <summary>Se > 0, acelera a apresentação (testes automáticos e playtest).</summary>
        public static float SpeedOverride;

        public static void Clear() { Setup = null; OnFinished = null; SpeedOverride = 0f; }
    }
}
