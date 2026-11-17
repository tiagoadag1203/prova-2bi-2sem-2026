using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace XequeDeTinta.Tests
{
    /// <summary>Faces do dado: chapadas, de frente, idênticas entre si, com o número certo de pontos.</summary>
    public class DiceFaceTests
    {
        static Texture2D Face(int n)
        {
            var bytes = System.IO.File.ReadAllBytes($"Assets/_Project/Art/Board/dice_face_{n}.png");
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.LoadImage(bytes);
            return t;
        }

        static bool IsPip(Color32 c) => c.r == 0x6B && c.g == 0x2F && c.b == 0x9C;

        [Test]
        public void TodasAsFacesTem32x32()
        {
            for (int n = 1; n <= 6; n++) { var t = Face(n); Assert.AreEqual((32, 32), (t.width, t.height), "face " + n); }
        }

        [Test]
        public void CadaFaceTemExatamenteNPontosRoxos()
        {
            for (int n = 1; n <= 6; n++)
            {
                var px = Face(n).GetPixels32();
                int pipPixels = px.Count(IsPip);
                Assert.AreEqual(n * 21, pipPixels, $"face {n}: cada ponto tem 21 px");
            }
        }

        [Test]
        public void ForaOsPontos_TodasAsFacesSaoIdenticas()
        {
            var baseFace = Face(1).GetPixels32();
            for (int n = 2; n <= 6; n++)
            {
                var px = Face(n).GetPixels32();
                for (int i = 0; i < px.Length; i++)
                {
                    bool a = IsPip(baseFace[i]), b = IsPip(px[i]);
                    if (a || b) continue;
                    Assert.AreEqual(baseFace[i], px[i], $"face {n}, pixel {i}: corpo do dado deve ser igual");
                }
            }
        }

        [Test]
        public void DadoEChapado_SemPerspectiva_ContornoEscuroECantosArredondados()
        {
            var t = Face(1);
            Assert.AreEqual(0, t.GetPixel(3, 3).a, 0.01f, "canto arredondado é transparente");
            Assert.AreEqual(1f, t.GetPixel(15, 3).a, 0.01f, "borda é opaca");
            Assert.AreEqual(0x2A, ((Color32)t.GetPixel(15, 28)).r, "contorno escuro no topo");
            // Simétrico na horizontal: o dado visto de frente não tem lado de perspectiva.
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 16; x++)
                    Assert.AreEqual(t.GetPixel(x + 3 - 3, y).a > 0, t.GetPixel(31 - x, y).a > 0, $"assimetria em {x},{y}");
        }

        [Test]
        public void DadosAntigosGuardadosEmRejected()
        {
            for (int n = 1; n <= 6; n++)
                Assert.IsTrue(System.IO.File.Exists($"Assets/_Project/Art/_Rejected/dice_face_{n}_cubo3d.png"), "face " + n);
        }
    }
}
