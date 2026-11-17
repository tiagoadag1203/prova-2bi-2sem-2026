using UnityEngine;

namespace XequeDeTinta.Core
{
    /// <summary>Todos os clipes do jogo. Fica em Resources/AudioLibrary e é preenchido por Xeque > M7 > Preencher AudioLibrary.</summary>
    [CreateAssetMenu(menuName = "Xeque de Tinta/AudioLibrary", fileName = "AudioLibrary")]
    public class AudioLibrary : ScriptableObject
    {
        public AudioClip dice, step, card, hit, hitEffective, shield, heal, burn, die, hurt, coin, click, win, lose, summon;
        public AudioClip musicBoard, musicBattle;

        public AudioClip Get(Sfx s)
        {
            switch (s)
            {
                case Sfx.Dice: return dice;
                case Sfx.Step: return step;
                case Sfx.Card: return card;
                case Sfx.Hit: return hit;
                case Sfx.HitEffective: return hitEffective;
                case Sfx.Shield: return shield;
                case Sfx.Heal: return heal;
                case Sfx.Burn: return burn;
                case Sfx.Die: return die;
                case Sfx.Hurt: return hurt;
                case Sfx.Coin: return coin;
                case Sfx.Click: return click;
                case Sfx.Win: return win;
                case Sfx.Lose: return lose;
                default: return summon;
            }
        }
    }
}
