#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using XequeDeTinta.UI;

namespace XequeDeTinta.EditorTools
{
    public static class UIArtBuilder
    {
        const string Path = "Assets/_Project/Resources/UIArt.asset";

        [MenuItem("Xeque/M6/Preencher UIArt")]
        public static void Build()
        {
            Directory.CreateDirectory("Assets/_Project/Resources");
            var art = AssetDatabase.LoadAssetAtPath<UIArt>(Path);
            if (art == null) { art = ScriptableObject.CreateInstance<UIArt>(); AssetDatabase.CreateAsset(art, Path); }

            // Ordem de Element: Neutro, Fogo, Planta, Água.
            art.elementIcons = new[]
            {
                SceneKit.Sprite("UI/Elements/element_neutral.png"), SceneKit.Sprite("UI/Elements/element_fire.png"),
                SceneKit.Sprite("UI/Elements/element_plant.png"), SceneKit.Sprite("UI/Elements/element_water.png"),
            };
            art.statusIcons = new[]
            {
                SceneKit.Sprite("UI/Status/status_shield.png"), SceneKit.Sprite("UI/Status/status_burn.png"),
                SceneKit.Sprite("UI/Status/status_weak.png"), SceneKit.Sprite("UI/Status/status_strength.png"),
                SceneKit.Sprite("UI/Status/status_stun.png"),
            };
            art.coin = SceneKit.Sprite("UI/coin.png");
            art.energyOrb = SceneKit.Sprite("UI/energy_orb.png");
            art.logo = SceneKit.Sprite("UI/logo.png");
            art.backgroundBoard = SceneKit.Sprite("Backgrounds/bg_board.png");
            art.backgroundBattle = SceneKit.Sprite("Backgrounds/bg_battle.png");
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            Debug.Log("[M6] UIArt preenchido.");
        }
    }
}
#endif
