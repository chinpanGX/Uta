using Cysharp.Threading.Tasks;
using UnityEngine;
using VolTech.Client;

namespace Uta.Client.Bootstrap
{
    /// <summary>
    /// 共通エラーハンドラー用の診断用ダイアログ（IMGUI）。画面の計画で作り直す。
    /// 診断用。同時に表示できるのは 1 つ（ApiCore がエラーハンドラーを順番待ちにしている前提）。
    /// Presentation の参照先に VolTech.MagicOnion.Client が無く、IErrorDialogView を実装できないため Bootstrap に置く。
    /// </summary>
    public sealed class ErrorDialogView : MonoBehaviour, IErrorDialogView
    {
        private bool autoDismiss;
        private UniTaskCompletionSource<ErrorDialogButton> source;
        private string message;
        private ErrorDialogButtons buttons;

        public void Initialize(bool autoDismiss)
        {
            this.autoDismiss = autoDismiss;
        }

        public UniTask<ErrorDialogButton> ShowAsync(ApiError error, ErrorDialogButtons buttons)
        {
            Debug.LogWarning("[Uta] error dialog: " + error + " buttons=" + buttons);
            if (autoDismiss)
            {
                switch (buttons)
                {
                    case ErrorDialogButtons.Ok:
                        return UniTask.FromResult(ErrorDialogButton.Ok);
                    case ErrorDialogButtons.RetryOrTitle:
                        return UniTask.FromResult(ErrorDialogButton.Title);
                    case ErrorDialogButtons.Home:
                        return UniTask.FromResult(ErrorDialogButton.Home);
                    case ErrorDialogButtons.Title:
                        return UniTask.FromResult(ErrorDialogButton.Title);
                    default:
                        return UniTask.FromResult(ErrorDialogButton.Store);
                }
            }

            source = new UniTaskCompletionSource<ErrorDialogButton>();
            message = error.ToString();
            this.buttons = buttons;
            return source.Task;
        }

        private void OnDestroy()
        {
            if (source != null)
            {
                var pending = source;
                source = null;
                pending.TrySetCanceled();
            }
        }

        private void OnGUI()
        {
            if (source == null)
            {
                return;
            }

            GUI.depth = -1;
            var boxStyle = new GUIStyle(GUI.skin.box);
            var buttonStyle = new GUIStyle(GUI.skin.button);
            var fontSize = Mathf.Max(14, Screen.height / 40);
            boxStyle.fontSize = fontSize;
            buttonStyle.fontSize = fontSize;
            boxStyle.wordWrap = true;
            boxStyle.normal.textColor = Color.white;
            GUI.Box(new Rect(16, Screen.height / 2, Screen.width - 32, Screen.height / 4), message, boxStyle);

            var n = buttons == ErrorDialogButtons.RetryOrTitle ? 2 : 1;
            var w = (Screen.width - 32 - 16 * (n - 1)) / n;
            for (var k = 0; k < n; k++)
            {
                var button = ButtonAt(k);
                var rect = new Rect(16 + k * (w + 16), Screen.height * 3 / 4 + 16, w, Screen.height / 12);
                if (GUI.Button(rect, ButtonText(button), buttonStyle))
                {
                    var pending = source;
                    source = null;
                    pending.TrySetResult(button);
                    return;
                }
            }
        }

        private ErrorDialogButton ButtonAt(int index)
        {
            switch (buttons)
            {
                case ErrorDialogButtons.Ok:
                    return ErrorDialogButton.Ok;
                case ErrorDialogButtons.RetryOrTitle:
                    return index == 0 ? ErrorDialogButton.Retry : ErrorDialogButton.Title;
                case ErrorDialogButtons.Home:
                    return ErrorDialogButton.Home;
                case ErrorDialogButtons.Title:
                    return ErrorDialogButton.Title;
                default:
                    return ErrorDialogButton.Store;
            }
        }

        private static string ButtonText(ErrorDialogButton button)
        {
            return button == ErrorDialogButton.Ok ? "OK" : button.ToString();
        }
    }
}
