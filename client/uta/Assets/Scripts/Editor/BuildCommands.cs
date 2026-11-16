using System.Linq;
using ZLinq;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Uta.Client.Editor
{
    public static class BuildCommands
    {
        /// <summary>Windows の IL2CPP のプレイヤーを Build/Windows/Uta.exe に作る。-batchmode -executeMethod Uta.Client.Editor.BuildCommands.BuildWindowsIl2Cpp で呼ぶ。</summary>
        public static void BuildWindowsIl2Cpp()
        {
            // Addressables の内容は今のビルドターゲット向けに作られるため、ターゲットが違えば止める
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
            {
                Debug.LogError("[Uta] build: active target is " + EditorUserBuildSettings.activeBuildTarget);
                EditorApplication.Exit(1);
                return;
            }

            // BuildAddressablesWithPlayerBuild は DoNotBuildWithPlayer なので、ここで明示的に作る
            AddressableAssetSettings.BuildPlayerContent(out var result);
            if (!string.IsNullOrEmpty(result.Error))
            {
                Debug.LogError("[Uta] addressables: " + result.Error);
                EditorApplication.Exit(1);
                return;
            }

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = "Build/Windows/Uta.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        /// <summary>Android の IL2CPP の APK を Build/Android/Uta.apk に作る。-batchmode -buildTarget Android -executeMethod Uta.Client.Editor.BuildCommands.BuildAndroidIl2Cpp で呼ぶ。</summary>
        public static void BuildAndroidIl2Cpp()
        {
            // Addressables の内容は今のビルドターゲット向けに作られるため、ターゲットが違えば止める
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.LogError("[Uta] build: active target is " + EditorUserBuildSettings.activeBuildTarget);
                EditorApplication.Exit(1);
                return;
            }

            AddressableAssetSettings.BuildPlayerContent(out var result);
            if (!string.IsNullOrEmpty(result.Error))
            {
                Debug.LogError("[Uta] addressables: " + result.Error);
                EditorApplication.Exit(1);
                return;
            }

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = "Build/Android/Uta.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None,
            });
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
