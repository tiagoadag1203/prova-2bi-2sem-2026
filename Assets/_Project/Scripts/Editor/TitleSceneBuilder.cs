#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using XequeDeTinta.Data;
using XequeDeTinta.UI;

namespace XequeDeTinta.EditorTools
{
    public static class TitleSceneBuilder
    {
        [MenuItem("Xeque/M4/Construir cena Title")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneKit.CreateCamera();

            var go = new GameObject("Title");
            var tc = go.AddComponent<TitleController>();
            tc.config = SceneKit.LoadAsset<GameConfig>("Assets/_Project/Data/GameConfig.asset");
            tc.decor = new[]
            {
                Decor("Characters/Hero/pawn_idle_0.png", "pawn", -5.5f),
                Decor("Enemies/slime_green_idle_0.png", "slime_green", 2.5f, 2f),
                Decor("Enemies/slime_red_idle_0.png", "slime_red", 5.0f, 2f),
                Decor("Enemies/slime_blue_idle_0.png", "slime_blue", 7.5f, 2f),
            };

            Directory.CreateDirectory(SceneKit.ScenesDir);
            EditorSceneManager.SaveScene(scene, SceneKit.ScenesDir + "Title.unity");
            BoardSceneBuilder.AddToBuild(SceneKit.ScenesDir + "Title.unity");
            // A cena Title é a primeira do build.
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            list.Sort((a, b) => (a.path.EndsWith("Title.unity") ? 0 : 1).CompareTo(b.path.EndsWith("Title.unity") ? 0 : 1));
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log("[M4] Cena Title criada.");
        }

        static TitleController.Decor Decor(string sprite, string controller, float x, float scale = 3f) => new TitleController.Decor
        {
            sprite = SceneKit.Sprite(sprite),
            animator = SceneKit.LoadAsset<RuntimeAnimatorController>($"Assets/_Project/Animations/{controller}/{controller}.controller"),
            position = new Vector2(x, -3.2f),
            scale = scale,
        };
    }
}
#endif
