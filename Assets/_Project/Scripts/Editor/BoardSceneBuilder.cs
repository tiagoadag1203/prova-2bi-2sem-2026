#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using XequeDeTinta.Board;
using XequeDeTinta.Data;

namespace XequeDeTinta.EditorTools
{
    public static class BoardSceneBuilder
    {
        [MenuItem("Xeque/M2/Construir cena Board")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneKit.CreateCamera();

            var go = new GameObject("Board");
            var bc = go.AddComponent<BoardController>();
            bc.config = SceneKit.LoadAsset<GameConfig>("Assets/_Project/Data/GameConfig.asset");

            var types = System.Enum.GetValues(typeof(TileType));
            bc.tileSprites = new Sprite[types.Length];
            foreach (TileType t in types)
                bc.tileSprites[(int)t] = SceneKit.Sprite("Board/tile_" + t.ToString().ToLowerInvariant() + ".png");

            bc.diceFaces = new Sprite[6];
            for (int i = 1; i <= 6; i++) bc.diceFaces[i - 1] = SceneKit.Sprite("Board/dice_face_" + i + ".png");

            bc.pawnSprite = SceneKit.Sprite("Characters/Hero/pawn_idle_0.png");
            bc.pawnAnimator = SceneKit.LoadAsset<RuntimeAnimatorController>("Assets/_Project/Animations/pawn/pawn.controller");

            Directory.CreateDirectory(SceneKit.ScenesDir);
            EditorSceneManager.SaveScene(scene, SceneKit.ScenesDir + "Board.unity");
            AddToBuild(SceneKit.ScenesDir + "Board.unity");
            Debug.Log("[M2] Cena Board criada.");
        }

        public static void AddToBuild(string path)
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (list.Exists(s => s.path == path)) return;
            list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
#endif
