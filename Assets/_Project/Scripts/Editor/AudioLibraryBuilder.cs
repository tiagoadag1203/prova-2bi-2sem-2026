#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using XequeDeTinta.Core;

namespace XequeDeTinta.EditorTools
{
    public static class AudioLibraryBuilder
    {
        const string Path = "Assets/_Project/Resources/AudioLibrary.asset";

        static AudioClip Sfx(string n) => SceneKit.LoadAsset<AudioClip>($"Assets/_Project/Audio/Sfx/sfx_{n}.wav");
        static AudioClip Music(string n) => SceneKit.LoadAsset<AudioClip>($"Assets/_Project/Audio/Music/music_{n}.wav");

        [MenuItem("Xeque/M7/Preencher AudioLibrary")]
        public static void Build()
        {
            Directory.CreateDirectory("Assets/_Project/Resources");
            var lib = AssetDatabase.LoadAssetAtPath<AudioLibrary>(Path);
            if (lib == null) { lib = ScriptableObject.CreateInstance<AudioLibrary>(); AssetDatabase.CreateAsset(lib, Path); }
            lib.dice = Sfx("dice"); lib.step = Sfx("step"); lib.card = Sfx("card"); lib.hit = Sfx("hit");
            lib.hitEffective = Sfx("hit_effective"); lib.shield = Sfx("shield"); lib.heal = Sfx("heal");
            lib.burn = Sfx("burn"); lib.die = Sfx("die"); lib.hurt = Sfx("hurt"); lib.coin = Sfx("coin");
            lib.click = Sfx("click"); lib.win = Sfx("win"); lib.lose = Sfx("lose"); lib.summon = Sfx("summon");
            lib.musicBoard = Music("board"); lib.musicBattle = Music("battle");
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            Debug.Log("[M7] AudioLibrary preenchida.");
        }
    }
}
#endif
