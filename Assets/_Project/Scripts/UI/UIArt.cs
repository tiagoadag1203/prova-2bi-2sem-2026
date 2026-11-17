using UnityEngine;
using XequeDeTinta.Data;

namespace XequeDeTinta.UI
{
    /// <summary>Sprites de interface compartilhados entre as telas. Fica em Resources/UIArt e é preenchido por Xeque > M6 > Preencher UIArt.</summary>
    [CreateAssetMenu(menuName = "Xeque de Tinta/UIArt", fileName = "UIArt")]
    public class UIArt : ScriptableObject
    {
        /// <summary>Índice = (int)Element: Neutro, Fogo, Planta, Água.</summary>
        public Sprite[] elementIcons;
        /// <summary>Escudo, Queimadura, Fraco, Força, Atordoado.</summary>
        public Sprite[] statusIcons;
        public Sprite coin, energyOrb, logo, backgroundBoard, backgroundBattle;

        static UIArt instance;
        public static UIArt Get()
        {
            if (instance == null) instance = Resources.Load<UIArt>("UIArt");
            return instance;
        }

        public Sprite ElementIcon(Element e)
        {
            int i = (int)e;
            return elementIcons != null && i >= 0 && i < elementIcons.Length ? elementIcons[i] : null;
        }
    }
}
