using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using XequeDeTinta.Board;
using XequeDeTinta.Core;
using XequeDeTinta.Data;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XequeDeTinta.Tests
{
    public class DiceFlowTests
    {
        static GameConfig Cfg()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/_Project/Data/GameConfig.asset");
#else
            return null;
#endif
        }

        [SetUp] public void Reset() { if (GameManager.Instance != null) Object.DestroyImmediate(GameManager.Instance.gameObject); }
        [TearDown] public void Cleanup() { if (GameManager.Instance != null) Object.DestroyImmediate(GameManager.Instance.gameObject); }

        [UnityTest]
        public IEnumerator Rolagem_MostraSoFacesDe1AteODado_E_NumeroGrandeAoParar()
        {
            Assert.AreEqual(3, Cfg().diceSides, "o dado do tabuleiro é 1 a 3");
            GameManager.Ensure(Cfg()).NewRun();
            SceneManager.LoadScene("Board");
            yield return null; yield return null;
            var board = Object.FindAnyObjectByType<BoardController>();
            board.Resolver = (t, done) => done();
            board.Rng = new ScriptedRng(2);
            var dice = GameObject.Find("Dado").GetComponent<SpriteRenderer>();

            var seen = new HashSet<string>();
            board.RollDice();
            float t0 = Time.realtimeSinceStartup;
            TMP_Text big = null;
            while (board.State == BoardState.Rolling && Time.realtimeSinceStartup - t0 < 5f)
            {
                seen.Add(dice.sprite.name);
                yield return null;
            }
            // Ao parar, a face mostrada é a tirada e o número grande aparece.
            yield return null;
            Assert.AreEqual("dice_face_2", dice.sprite.name);
            var go = GameObject.Find("NumeroDado");
            Assert.IsNotNull(go);
            big = go.GetComponent<TMP_Text>();
            Assert.IsTrue(go.activeInHierarchy);
            Assert.AreEqual("2", big.text);

            foreach (var name in seen)
                CollectionAssert.Contains(new[] { "dice_face_1", "dice_face_2", "dice_face_3" }, name, "a rolagem só pode mostrar faces de 1 a 3");
            Assert.Greater(seen.Count, 1, "a rolagem alterna as faces");

            // Some depois de ~1 s.
            float t1 = Time.realtimeSinceStartup;
            while (go.activeInHierarchy && Time.realtimeSinceStartup - t1 < 3f) yield return null;
            Assert.IsFalse(go.activeInHierarchy);
        }
    }
}
