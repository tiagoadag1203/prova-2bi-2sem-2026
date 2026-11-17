using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using XequeDeTinta.Data;
using XequeDeTinta.UI;

namespace XequeDeTinta.Tests
{
    /// <summary>A arte do lote 1 e do lote 2 está ligada aos dados e às animações.</summary>
    public class ArtCompletenessTests
    {
        static GameConfig Cfg() => AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/_Project/Data/GameConfig.asset");

        [Test]
        public void TodoInimigoTemSpriteEAnimator()
        {
            var all = AssetDatabase.FindAssets("t:EnemyData", new[] { "Assets/_Project/Data/Enemies" })
                .Select(g => AssetDatabase.LoadAssetAtPath<EnemyData>(AssetDatabase.GUIDToAssetPath(g))).ToList();
            Assert.AreEqual(5, all.Count);
            foreach (var e in all)
            {
                Assert.IsNotNull(e.sprite, e.id + " sem sprite");
                Assert.IsNotNull(e.animator, e.id + " sem animator");
            }
        }

        [Test]
        public void TodaCartaTemIlustracao()
        {
            foreach (var c in Cfg().starterDeck.Concat(Cfg().rewardPool).Distinct())
                Assert.IsNotNull(c.art, c.id + " sem ilustração");
        }

        [Test]
        public void TodaRelicaTemIcone()
        {
            foreach (var r in Cfg().relics) Assert.IsNotNull(r.icon, r.id);
        }

        [Test]
        public void UIArtEstaCompleto()
        {
            var art = AssetDatabase.LoadAssetAtPath<UIArt>("Assets/_Project/Resources/UIArt.asset");
            Assert.IsNotNull(art);
            Assert.AreEqual(4, art.elementIcons.Length); Assert.IsTrue(art.elementIcons.All(s => s != null));
            Assert.AreEqual(5, art.statusIcons.Length); Assert.IsTrue(art.statusIcons.All(s => s != null));
            Assert.IsNotNull(art.coin); Assert.IsNotNull(art.energyOrb); Assert.IsNotNull(art.logo);
            Assert.IsNotNull(art.backgroundBoard); Assert.IsNotNull(art.backgroundBattle, "bg_battle.png corrompido ou não importado");
        }

        [Test]
        public void IconesDeIntencaoExistem()
        {
            foreach (var n in new[] { "attack", "defend", "buff", "debuff", "summon" })
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/_Project/Art/UI/Intents/intent_{n}.png"), n);
        }

        [TestCase("pawn")]
        [TestCase("slime_green")]
        [TestCase("slime_red")]
        [TestCase("slime_blue")]
        [TestCase("king_slime")]
        public void ClipeDeAtaqueDisparaOnImpactNoQuadro4(string who)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"Assets/_Project/Animations/{who}/{who}_attack.anim");
            Assert.IsNotNull(clip);
            var ev = AnimationUtility.GetAnimationEvents(clip);
            Assert.AreEqual(1, ev.Length);
            Assert.AreEqual("OnImpact", ev[0].functionName);
            Assert.AreEqual(3f / clip.frameRate, ev[0].time, 1e-4f, "índice 3 = quadro 4");
        }

        [TestCase("pawn")]
        [TestCase("slime_green")]
        [TestCase("king_slime")]
        public void ClipeDeMorteAvisaOFim(string who)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"Assets/_Project/Animations/{who}/{who}_death.anim");
            var ev = AnimationUtility.GetAnimationEvents(clip);
            Assert.AreEqual(1, ev.Length);
            Assert.AreEqual("OnDeathEnd", ev[0].functionName);
        }

        [Test]
        public void ReiSlimeTemTodasAsAnimacoesInclusiveInvocar()
        {
            foreach (var a in new[] { "idle", "attack", "summon", "hit", "death" })
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<AnimationClip>($"Assets/_Project/Animations/king_slime/king_slime_{a}.anim"), a);
            var ctrl = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/_Project/Animations/king_slime/king_slime.controller");
            Assert.IsTrue(ctrl.parameters.Any(p => p.name == "summon"));
            var king = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Project/Data/Enemies/Enemy_ReiSlime.asset");
            Assert.AreSame(ctrl, king.animator);
        }

        [Test]
        public void ReiSlimeUsaSprite64x64()
        {
            var king = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Project/Data/Enemies/Enemy_ReiSlime.asset");
            Assert.AreEqual(64, (int)king.sprite.rect.width);
            Assert.AreEqual(64, (int)king.sprite.rect.height);
            Assert.AreEqual(32f, king.sprite.pixelsPerUnit);
        }

        [Test]
        public void ScenesDoJogoEstaoNoBuild()
        {
            var scenes = EditorBuildSettings.scenes.Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path)).ToList();
            foreach (var n in new[] { "Title", "Board", "Battle", "End" }) CollectionAssert.Contains(scenes, n);
            Assert.AreEqual("Title", scenes[0], "o jogo abre pelo título");
        }
    }
}
