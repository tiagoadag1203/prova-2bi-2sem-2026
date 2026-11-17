#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using XequeDeTinta.Battle;
using XequeDeTinta.Data;

namespace XequeDeTinta.EditorTools
{
    public static class BattleSceneBuilder
    {
        [MenuItem("Xeque/M3/Construir cena Battle")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneKit.CreateCamera();

            var go = new GameObject("Battle");
            var bc = go.AddComponent<BattleController>();
            bc.config = SceneKit.LoadAsset<GameConfig>("Assets/_Project/Data/GameConfig.asset");
            bc.heroSprite = SceneKit.Sprite("Characters/Hero/pawn_idle_0.png");
            bc.heroAnimator = SceneKit.LoadAsset<RuntimeAnimatorController>("Assets/_Project/Animations/pawn/pawn.controller");

            var intents = System.Enum.GetValues(typeof(IntentType));
            bc.intentSprites = new Sprite[intents.Length];
            foreach (IntentType t in intents)
                bc.intentSprites[(int)t] = SceneKit.Sprite("UI/Intents/intent_" + t.ToString().ToLowerInvariant() + ".png");

            Directory.CreateDirectory(SceneKit.ScenesDir);
            EditorSceneManager.SaveScene(scene, SceneKit.ScenesDir + "Battle.unity");
            BoardSceneBuilder.AddToBuild(SceneKit.ScenesDir + "Battle.unity");
            Debug.Log("[M3] Cena Battle criada.");
        }
    }
}
#endif
