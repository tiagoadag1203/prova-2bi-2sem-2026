#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace XequeDeTinta.EditorTools
{
    /// <summary>Gera as builds de Mac e Windows em Build/ com as cenas do Build Settings (menu Xeque > Build).</summary>
    public static class BuildTools
    {
        static void Build(BuildTarget target, string path)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes, locationPathName = path, target = target, options = BuildOptions.None,
            });
            Debug.Log($"[Build] {target}: {report.summary.result}, {report.summary.totalErrors} erro(s), {report.summary.totalSize / 1048576} MB -> {path}");
            if (report.summary.result != BuildResult.Succeeded) throw new System.Exception("Build falhou: " + target);
        }

        [MenuItem("Xeque/Build/Mac")]
        public static void BuildMac() => Build(BuildTarget.StandaloneOSX, "Build/Mac/XequeDeTinta.app");

        [MenuItem("Xeque/Build/Windows")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Build/Windows/XequeDeTinta.exe");
    }
}
#endif
