#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace XequeDeTinta.EditorTools
{
    /// <summary>Peças comuns dos construtores de cena.</summary>
    public static class SceneKit
    {
        public const string ScenesDir = "Assets/_Project/Scenes/";

        /// <summary>Câmera fixa ortográfica com Pixel Perfect Camera (640x360, PPU 32) e fundo roxo escuro.</summary>
        public static Camera CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 360f / 2f / 32f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            ColorUtility.TryParseHtmlString("#2A1B3D", out var bg);
            cam.backgroundColor = bg;
            go.transform.position = new Vector3(0, 0, -10);
            go.AddComponent<UniversalAdditionalCameraData>();
            var ppc = go.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = 32;
            ppc.refResolutionX = 640;
            ppc.refResolutionY = 360;
            ppc.cropFrame = PixelPerfectCamera.CropFrame.None;
            ppc.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;
            return cam;
        }

        public static T LoadAsset<T>(string path) where T : Object
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) Debug.LogError("[SceneKit] Asset não encontrado: " + path);
            return a;
        }

        public static Sprite Sprite(string artRelativePath) =>
            LoadAsset<Sprite>("Assets/_Project/Art/" + artRelativePath);

    }
}
#endif
