#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace XequeDeTinta.EditorTools
{
    /// <summary>
    /// Aplica as regras de pixel art (CLAUDE.md) a toda textura em Art/ e gera clipes e Animators
    /// a partir de quadros nomeados nome_animacao_N.png.
    /// </summary>
    public class ArtPostprocessor : AssetPostprocessor
    {
        const string ArtRoot = "Assets/_Project/Art/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot) || assetPath.Contains("/_Reference/") || assetPath.Contains("/_Rejected/"))
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            bool character = assetPath.Contains("/Characters/") || assetPath.Contains("/Enemies/") || assetPath.Contains("/Bosses/");
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)(character ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
            importer.SetTextureSettings(settings);
        }
    }

    public static class ArtPipeline
    {
        static readonly Regex FramePattern = new Regex(@"^(?<name>.+)_(?<anim>[a-z]+)_(?<n>\d+)\.png$");
        static readonly string[] CharacterRoots =
        {
            "Assets/_Project/Art/Characters",
            "Assets/_Project/Art/Enemies",
            "Assets/_Project/Art/Bosses",
        };

        // Animações em loop; as demais tocam uma vez.
        static readonly HashSet<string> Loops = new HashSet<string> { "idle" };

        /// <summary>Índice (base 0) do quadro de impacto do ataque; o Animation Event OnImpact dispara nele (quadro 4 de 6).</summary>
        const int ImpactFrame = 3;

        [MenuItem("Xeque/Reimportar arte")]
        public static void Reimport()
        {
            AssetDatabase.ImportAsset("Assets/_Project/Art", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        }

        [MenuItem("Xeque/Gerar animações")]
        public static void BuildAnimations()
        {
            foreach (var root in CharacterRoots)
            {
                if (!Directory.Exists(root)) continue;
                var groups = new Dictionary<string, SortedDictionary<string, SortedDictionary<int, string>>>();
                foreach (var file in Directory.GetFiles(root, "*.png", SearchOption.AllDirectories))
                {
                    var m = FramePattern.Match(Path.GetFileName(file));
                    if (!m.Success) continue;
                    string name = m.Groups["name"].Value, anim = m.Groups["anim"].Value;
                    int n = int.Parse(m.Groups["n"].Value);
                    if (!groups.TryGetValue(name, out var anims))
                        groups[name] = anims = new SortedDictionary<string, SortedDictionary<int, string>>();
                    if (!anims.TryGetValue(anim, out var frames))
                        anims[anim] = frames = new SortedDictionary<int, string>();
                    frames[n] = file.Replace('\\', '/');
                }
                foreach (var kv in groups) BuildCharacter(kv.Key, kv.Value);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void BuildCharacter(string name, SortedDictionary<string, SortedDictionary<int, string>> anims)
        {
            string dir = "Assets/_Project/Animations/" + name;
            Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();

            // Atualiza o controller no lugar (mantém o GUID, para não quebrar referências em dados e cenas).
            string controllerPath = dir + "/" + name + ".controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)
                             ?? AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var sm = controller.layers[0].stateMachine;
            foreach (var st in sm.states) sm.RemoveState(st.state);
            foreach (var tr in sm.anyStateTransitions) sm.RemoveAnyStateTransition(tr);
            for (int i = controller.parameters.Length - 1; i >= 0; i--) controller.RemoveParameter(i);

            foreach (var a in anims)
            {
                var sprites = new List<Sprite>();
                foreach (var f in a.Value)
                {
                    var s = AssetDatabase.LoadAssetAtPath<Sprite>(f.Value);
                    if (s != null) sprites.Add(s);
                }
                if (sprites.Count == 0) continue;

                var clip = new AnimationClip { frameRate = 8 };
                var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
                var keys = new ObjectReferenceKeyframe[sprites.Count];
                for (int i = 0; i < keys.Length; i++)
                    keys[i] = new ObjectReferenceKeyframe { time = i / clip.frameRate, value = sprites[i] };
                AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
                var cs = AnimationUtility.GetAnimationClipSettings(clip);
                cs.loopTime = Loops.Contains(a.Key);
                AnimationUtility.SetAnimationClipSettings(clip, cs);

                // Animation Events: o dano é aplicado no impacto; ao fim da morte o corpo some.
                if ((a.Key == "attack" || a.Key == "summon") && sprites.Count > ImpactFrame)
                    AnimationUtility.SetAnimationEvents(clip, new[]
                    {
                        new AnimationEvent { time = ImpactFrame / clip.frameRate, functionName = "OnImpact" },
                    });
                else if (a.Key == "death")
                    AnimationUtility.SetAnimationEvents(clip, new[]
                    {
                        new AnimationEvent { time = (sprites.Count - 1) / clip.frameRate, functionName = "OnDeathEnd" },
                    });

                string clipPath = dir + "/" + name + "_" + a.Key + ".anim";
                AssetDatabase.DeleteAsset(clipPath);
                AssetDatabase.CreateAsset(clip, clipPath);

                var state = sm.AddState(a.Key);
                state.motion = clip;
                if (a.Key == "idle") sm.defaultState = state;
            }

            // Triggers e transições: Any State -> animação; animação -> idle ao terminar (exceto death).
            AnimatorState idle = null;
            foreach (var s in sm.states) if (s.state.name == "idle") idle = s.state;
            foreach (var s in sm.states)
            {
                string n = s.state.name;
                if (n == "idle") continue;
                controller.AddParameter(n, AnimatorControllerParameterType.Trigger);
                var t = sm.AddAnyStateTransition(s.state);
                t.AddCondition(AnimatorConditionMode.If, 0, n);
                t.hasExitTime = false; t.duration = 0; t.canTransitionToSelf = true;
                if (n != "death" && idle != null)
                {
                    var back = s.state.AddTransition(idle);
                    back.hasExitTime = true; back.exitTime = 1f; back.duration = 0;
                }
            }
            EditorUtility.SetDirty(controller);
        }
    }
}
#endif
