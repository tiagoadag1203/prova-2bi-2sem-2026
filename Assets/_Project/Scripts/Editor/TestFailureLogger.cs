#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace XequeDeTinta.EditorTools
{
    /// <summary>Registra no Console o motivo de cada teste que falha ([TESTFAIL]); a ferramenta de testes só devolve os nomes.</summary>
    [InitializeOnLoad]
    public static class TestFailureLogger
    {
        class Callbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void RunFinished(ITestResultAdaptor result) { }
            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.HasChildren || result.TestStatus != TestStatus.Failed) return;
                Debug.Log($"[TESTFAIL] {result.Test.FullName}\n{result.Message}\n{result.StackTrace}");
            }
        }

        static TestFailureLogger()
        {
            ScriptableObject.CreateInstance<TestRunnerApi>().RegisterCallbacks(new Callbacks());
        }
    }
}
#endif
