using UnityEngine;
using UnityEngine.SceneManagement;

namespace XequeDeTinta.Core
{
    public enum Sfx { Dice, Step, Card, Hit, HitEffective, Shield, Heal, Burn, Die, Hurt, Coin, Click, Win, Lose, Summon }

    /// <summary>
    /// Efeitos (pool de AudioSources) e música em loop trocada por cena: Battle toca a de batalha;
    /// Title, Board e End tocam a do tabuleiro. O volume geral é o AudioListener.volume (controle do título).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        static AudioManager instance;
        AudioLibrary library;
        AudioSource music;
        AudioSource[] pool;
        int next;

        public const float MusicVolume = 0.45f, SfxVolume = 0.85f;

        static AudioManager Get()
        {
            if (instance == null)
            {
                var go = new GameObject("AudioManager");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<AudioManager>();
            }
            return instance;
        }

        /// <summary>Cria o gerenciador (e começa a música da cena atual). Chamado pelo GameManager.</summary>
        public static void Ensure() => Get();

        public static void Play(Sfx sfx)
        {
            var m = Get();
            if (m.library == null) return;
            var clip = m.library.Get(sfx);
            if (clip == null) return;
            var src = m.pool[m.next++ % m.pool.Length];
            src.PlayOneShot(clip, SfxVolume);
        }

        /// <summary>Qual música deve tocar numa cena.</summary>
        public static string TrackFor(string scene) => scene == "Battle" ? "battle" : "board";

        public string CurrentTrack { get; private set; }
        /// <summary>Faixa de música tocando agora ("board" ou "battle"); null se o gerenciador ainda não existe.</summary>
        public static string CurrentTrackName => instance != null ? instance.CurrentTrack : null;

        void Awake()
        {
            library = Resources.Load<AudioLibrary>("AudioLibrary");
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true; music.playOnAwake = false; music.volume = MusicVolume;
            pool = new AudioSource[8];
            for (int i = 0; i < pool.Length; i++)
            {
                pool[i] = gameObject.AddComponent<AudioSource>();
                pool[i].playOnAwake = false;
            }
            SceneManager.sceneLoaded += (s, _) => PlayMusicFor(s.name);
            PlayMusicFor(SceneManager.GetActiveScene().name);
        }

        void OnDestroy() { if (instance == this) instance = null; }

        void PlayMusicFor(string scene)
        {
            if (library == null) return;
            string track = TrackFor(scene);
            if (track == CurrentTrack && music.isPlaying) return;
            var clip = track == "battle" ? library.musicBattle : library.musicBoard;
            if (clip == null) return;
            CurrentTrack = track;
            music.clip = clip;
            music.Play();
        }
    }
}
