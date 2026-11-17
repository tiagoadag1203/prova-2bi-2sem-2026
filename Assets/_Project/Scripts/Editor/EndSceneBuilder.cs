#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using XequeDeTinta.Data;
using XequeDeTinta.UI;

namespace XequeDeTinta.EditorTools
{
    public static class EndSceneBuilder
    {
        [MenuItem("Xeque/M6/Construir cena End")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneKit.CreateCamera();
            var go = new GameObject("End");
            var ec = go.AddComponent<EndController>();
            ec.config = SceneKit.LoadAsset<GameConfig>("Assets/_Project/Data/GameConfig.asset");
            ec.background = SceneKit.Sprite("Backgrounds/bg_board.png");
            ec.pawnSprite = SceneKit.Sprite("Characters/Hero/pawn_idle_0.png");
            ec.pawnAnimator = SceneKit.LoadAsset<RuntimeAnimatorController>("Assets/_Project/Animations/pawn/pawn.controller");
            Directory.CreateDirectory(SceneKit.ScenesDir);
            EditorSceneManager.SaveScene(scene, SceneKit.ScenesDir + "End.unity");
            BoardSceneBuilder.AddToBuild(SceneKit.ScenesDir + "End.unity");
            Debug.Log("[M6] Cena End criada.");
        }
    }
}
#endif
