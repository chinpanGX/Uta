using UnityEngine;

namespace Uta.Client.Presentation.Startup
{
    /// <summary>疎通の確認の結果を IMGUI で出す診断用の表示。画面の計画で作り直す。</summary>
    public sealed class StartupView : MonoBehaviour
    {
        private string text = string.Empty;

        public void SetText(string value)
        {
            text = value;
        }

        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.wordWrap = true;
            style.fontSize = Mathf.Max(14, Screen.height / 40);
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(16, 16, Screen.width - 32, Screen.height - 32), text, style);
        }
    }
}
