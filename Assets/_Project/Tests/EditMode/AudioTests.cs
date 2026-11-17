using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using XequeDeTinta.Core;

namespace XequeDeTinta.Tests
{
    public class AudioTests
    {
        static AudioLibrary Lib() => AssetDatabase.LoadAssetAtPath<AudioLibrary>("Assets/_Project/Resources/AudioLibrary.asset");

        [Test]
        public void BibliotecaTemTodosOsSonsDoGdd()
        {
            var lib = Lib();
            Assert.IsNotNull(lib, "rode Xeque > M7 > Preencher AudioLibrary");
            foreach (Sfx s in System.Enum.GetValues(typeof(Sfx)))
            {
                var clip = lib.Get(s);
                Assert.IsNotNull(clip, "falta o efeito " + s);
                Assert.Greater(clip.length, 0.03f, s.ToString());
                Assert.Less(clip.length, 3f, s + " longo demais para um efeito");
            }
        }

        [Test]
        public void MusicasSaoLoopsLongosDeVerdade()
        {
            Assert.Greater(Lib().musicBoard.length, 20f);
            Assert.Greater(Lib().musicBattle.length, 15f);
        }

        [Test]
        public void BatalhaTocaAMusicaDeBatalhaEOResto_ATrilhaDoTabuleiro()
        {
            Assert.AreEqual("battle", AudioManager.TrackFor("Battle"));
            foreach (var n in new[] { "Title", "Board", "End" }) Assert.AreEqual("board", AudioManager.TrackFor(n));
        }

        [Test]
        public void LicencasDeAudioRegistradas()
        {
            var txt = System.IO.File.ReadAllText("Assets/_Project/Audio/LICENSES.md");
            StringAssert.Contains("CC0", txt);
            StringAssert.Contains("sfx_", txt);
            StringAssert.Contains("music_battle", txt);
            StringAssert.Contains("Press Start 2P", txt);
        }

        [Test]
        public void TodoArquivoDeAudioEstaNaLicencaOuNoGerador()
        {
            var files = System.IO.Directory.GetFiles("Assets/_Project/Audio", "*.wav", System.IO.SearchOption.AllDirectories);
            Assert.AreEqual(17, files.Length);
        }
    }
}
