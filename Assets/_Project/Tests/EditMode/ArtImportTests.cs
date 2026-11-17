using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace XequeDeTinta.Tests
{
    /// <summary>Garante as regras de pixel art do CLAUDE.md em toda textura do jogo.</summary>
    public class ArtImportTests
    {
        static string[] GamePngs()
        {
            var all = Directory.GetFiles("Assets/_Project/Art", "*.png", SearchOption.AllDirectories);
            return System.Array.FindAll(all, p =>
            {
                p = p.Replace('\\', '/');
                return !p.Contains("/_Reference/") && !p.Contains("/_Rejected/");
            });
        }

        [Test]
        public void TodaTexturaUsaPixelArtSettings()
        {
            var files = GamePngs();
            Assert.IsNotEmpty(files, "Nenhuma textura encontrada em Art/");
            foreach (var f in files)
            {
                var path = f.Replace('\\', '/');
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.IsNotNull(imp, path);
                Assert.AreEqual(32f, imp.spritePixelsPerUnit, path + " PPU");
                Assert.AreEqual(FilterMode.Point, imp.filterMode, path + " filtro");
                Assert.AreEqual(TextureImporterCompression.Uncompressed, imp.textureCompression, path + " compressão");
                Assert.IsFalse(imp.mipmapEnabled, path + " mipmaps");
            }
        }

        [Test]
        public void TodoAnimadorTemTodasAsAnimacoes()
        {
            string[] controllers = { "pawn", "slime_green", "slime_red", "slime_blue" };
            string[] anims = { "idle", "attack", "hit", "death" };
            foreach (var c in controllers)
                foreach (var a in anims)
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"Assets/_Project/Animations/{c}/{c}_{a}.anim");
                    Assert.IsNotNull(clip, $"{c}_{a}");
                }
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Animations/pawn/pawn_hop.anim"));
        }
    }
}
