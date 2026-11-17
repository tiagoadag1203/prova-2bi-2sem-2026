#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace XequeDeTinta.EditorTools
{
    public static class FontSetup
    {
        const string FontPath = "Assets/_Project/Art/UI/Fonts/PressStart2P-Regular.ttf";
        const string AssetPath = "Assets/_Project/Resources/Fonts/PressStart2P SDF.asset";

        [MenuItem("Xeque/Criar fonte TMP")]
        public static void Build()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null) { Debug.LogError("Fonte não encontrada: " + FontPath); return; }
            AssetDatabase.DeleteAsset(AssetPath);

            var fa = TMP_FontAsset.CreateFontAsset(font, 16, 4, GlyphRenderMode.SDFAA, 512, 512, AtlasPopulationMode.Dynamic, true);
            fa.name = "PressStart2P SDF";
            AssetDatabase.CreateAsset(fa, AssetPath);
            fa.atlasTexture.name = "PressStart2P Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);
            fa.material.name = "PressStart2P Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);

            var chars = new System.Text.StringBuilder();
            for (char c = ' '; c <= '~'; c++) chars.Append(c);
            chars.Append("áàâãäéèêíìîóòôõöúùûüçñÁÀÂÃÄÉÈÊÍÌÎÓÒÔÕÖÚÙÛÜÇÑ·•–—…“”‘’«»×÷°ºª¡¿€");
            fa.TryAddCharacters(chars.ToString(), out var missing);
            if (!string.IsNullOrEmpty(missing)) Debug.LogWarning("[Fonte] sem glifo para: " + missing);

            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            Debug.Log("[Fonte] Criada: " + AssetPath);
        }
    }
}
#endif
