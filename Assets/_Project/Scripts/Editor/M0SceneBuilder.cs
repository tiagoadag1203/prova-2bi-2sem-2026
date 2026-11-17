#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace XequeDeTinta.EditorTools
{
    public static class M0SceneBuilder
    {
        const string Art = "Assets/_Project/Art/";

        [MenuItem("Xeque/M0/Construir cena de teste")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Câmera fixa ortográfica com Pixel Perfect (640x360, PPU 32).
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 360f / 2f / 32f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            ColorUtility.TryParseHtmlString("#2A1B3D", out var bg);
            cam.backgroundColor = bg;
            camGo.transform.position = new Vector3(0, 0, -10);
            camGo.AddComponent<UniversalAdditionalCameraData>();
            var ppc = camGo.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = 32;
            ppc.refResolutionX = 640;
            ppc.refResolutionY = 360;
            ppc.cropFrame = PixelPerfectCamera.CropFrame.None;
            ppc.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;

            // Personagens animados (linha de cima).
            Animated("Peao", "Characters/Hero/pawn_idle_0.png", "pawn", new Vector3(-6f, -1f), false, 0f);
            Animated("SlimeVerde", "Enemies/slime_green_idle_0.png", "slime_green", new Vector3(-2f, -1f), false, 0.4f);
            Animated("SlimeVermelho", "Enemies/slime_red_idle_0.png", "slime_red", new Vector3(2f, -1f), false, 0.8f);
            Animated("SlimeAzul", "Enemies/slime_blue_idle_0.png", "slime_blue", new Vector3(6f, -1f), false, 1.2f);

            // Casas.
            string[] tiles = { "start", "battle", "shop", "rest", "event", "chest", "boss" };
            for (int i = 0; i < tiles.Length; i++)
                Static("Casa_" + tiles[i], "Board/tile_" + tiles[i] + ".png", new Vector3(-7.5f + i * 1.5f, 3.2f));

            // Dado.
            for (int i = 1; i <= 6; i++)
                Static("Dado_" + i, "Board/dice_face_" + i + ".png", new Vector3(-3.75f + (i - 1) * 1.5f, 1.4f));

            // Intenções.
            string[] intents = { "attack", "defend", "buff", "debuff", "summon" };
            for (int i = 0; i < intents.Length; i++)
                Static("Intencao_" + intents[i], "UI/Intents/intent_" + intents[i] + ".png", new Vector3(-3f + i * 1.5f, -3.4f));

            Directory.CreateIfMissing();
            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/M0_Test.unity");
            Debug.Log("[M0] Cena de teste criada em Assets/_Project/Scenes/M0_Test.unity");
        }

        static GameObject Static(string name, string spritePath, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + spritePath);
            return go;
        }

        static void Animated(string name, string spritePath, string controller, Vector3 pos, bool flip, float delay)
        {
            var go = Static(name, spritePath, pos);
            go.GetComponent<SpriteRenderer>().flipX = flip;
            var animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController =
                AssetDatabase.LoadAssetAtPath<AnimatorController>($"Assets/_Project/Animations/{controller}/{controller}.controller");
            var cycler = go.AddComponent<AnimationDemoCycler>();
            cycler.startDelay = delay;
        }

        static class Directory
        {
            public static void CreateIfMissing() =>
                System.IO.Directory.CreateDirectory("Assets/_Project/Scenes");
        }
    }
}
#endif
