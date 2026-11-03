using UnityEditor;
using UnityEditor.SceneManagement;

namespace Uta.Client.Editor
{
    /// <summary>Play モードを、いま開いているシーンではなく Bootstrap シーンから始める。</summary>
    [InitializeOnLoad]
    internal static class BootstrapPlayMode
    {
        static BootstrapPlayMode()
        {
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Bootstrap.unity");
        }
    }
}
