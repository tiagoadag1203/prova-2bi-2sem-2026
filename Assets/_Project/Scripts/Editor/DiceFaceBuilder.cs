#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace XequeDeTinta.EditorTools
{
    /// <summary>
    /// Escreve as 6 faces do dado como PNGs chapados (vistos de frente, sem perspectiva), idênticos entre si:
    /// quadrado creme de cantos arredondados, contorno escuro de 1 px e pontos roxos na posição clássica.
    /// </summary>
    public static class DiceFaceBuilder
    {
        const int S = 32;
        static readonly Color32 Cream = new Color32(0xF4, 0xE9, 0xD8, 0xFF);
        static readonly Color32 Shade = new Color32(0xD9, 0xC8, 0xAE, 0xFF);
        static readonly Color32 Outline = new Color32(0x2A, 0x1B, 0x3D, 0xFF);
        static readonly Color32 Pip = new Color32(0x6B, 0x2F, 0x9C, 0xFF);

        // Caixa do dado: x e y de 3 a 28 (26 px); raio dos cantos 4.
        const int Lo = 3, Hi = 28, R = 4;
        // Centros dos pontos (pontos de 5x5 px).
        static readonly int[] C = { 10, 16, 21 };

        static readonly int[][][] Layout =
        {
            new[] { new[] { 1, 1 } },
            new[] { new[] { 0, 2 }, new[] { 2, 0 } },
            new[] { new[] { 0, 2 }, new[] { 1, 1 }, new[] { 2, 0 } },
            new[] { new[] { 0, 0 }, new[] { 0, 2 }, new[] { 2, 0 }, new[] { 2, 2 } },
            new[] { new[] { 0, 0 }, new[] { 0, 2 }, new[] { 1, 1 }, new[] { 2, 0 }, new[] { 2, 2 } },
            new[] { new[] { 0, 0 }, new[] { 0, 2 }, new[] { 1, 0 }, new[] { 1, 2 }, new[] { 2, 0 }, new[] { 2, 2 } },
        };

        static bool Inside(int x, int y, int inset)
        {
            int lo = Lo + inset, hi = Hi - inset, r = Mathf.Max(0, R - inset);
            if (x < lo || x > hi || y < lo || y > hi) return false;
            int dx = x < lo + r ? lo + r - x : (x > hi - r ? x - (hi - r) : 0);
            int dy = y < lo + r ? lo + r - y : (y > hi - r ? y - (hi - r) : 0);
            return dx * dx + dy * dy <= r * r + r / 2;
        }

        [MenuItem("Xeque/Dado/Gerar faces chapadas")]
        public static void Build()
        {
            const string dir = "Assets/_Project/Art/Board/";
            const string rej = "Assets/_Project/Art/_Rejected/";
            for (int n = 1; n <= 6; n++)
            {
                string path = dir + $"dice_face_{n}.png";
                string old = rej + $"dice_face_{n}_cubo3d.png";
                if (File.Exists(path) && !File.Exists(old)) File.Copy(path, old);   // guarda o cubo 3D antigo

                var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
                var clear = new Color32(0, 0, 0, 0);
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                    {
                        Color32 c = clear;
                        if (Inside(x, y, 0)) c = Outline;
                        if (Inside(x, y, 1)) c = (y <= Lo + 2) ? Shade : Cream;   // faixa de sombra de 1 px embaixo
                        tex.SetPixel(x, y, c);
                    }
                foreach (var p in Layout[n - 1])
                {
                    int cx = C[p[0]], cy = C[p[1]];
                    for (int dy = -2; dy <= 2; dy++)
                        for (int dx = -2; dx <= 2; dx++)
                            if (Mathf.Abs(dx) + Mathf.Abs(dy) < 4) tex.SetPixel(cx + dx, cy + dy, Pip);
                }
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            AssetDatabase.Refresh();
            Debug.Log("[Dado] Faces chapadas geradas em " + dir);
        }
    }
}
#endif
