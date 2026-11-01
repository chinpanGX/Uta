using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Uta.Client.Editor
{
    public static class BuildCommands
    {
        /// <summary>Windows の IL2CPP のプレイヤーを Build/Windows/Uta.exe に作る。-batchmode -executeMethod Uta.Client.Editor.BuildCommands.BuildWindowsIl2Cpp で呼ぶ。</summary>
        public static void BuildWindowsIl2Cpp()
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/SampleScene.unity" },
                locationPathName = "Build/Windows/Uta.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
