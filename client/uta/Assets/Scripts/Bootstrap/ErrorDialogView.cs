using Cysharp.Threading.Tasks;
using UnityEngine;
using Uta.Client.Presentation.Common;
using VolTech.Client;

namespace Uta.Client.Bootstrap
{
    /// <summary>
    /// 共通エラーハンドラー用のダイアログ。表示はいまのシーンの DialogOpener に任せる。
    /// Presentation の参照先に VolTech.MagicOnion.Client が無く、IErrorDialogView を実装できないため Bootstrap に置く。
    /// </summary>
    public sealed class ErrorDialogView : IErrorDialogView
    {
        private readonly bool autoDismiss;
        private readonly ActiveSceneUi activeSceneUi;

        public ErrorDialogView(bool autoDismiss, ActiveSceneUi activeSceneUi)
        {
            this.autoDismiss = autoDismiss;
            this.activeSceneUi = activeSceneUi;
        }

        public async UniTask<ErrorDialogButton> ShowAsync(ApiError error, ErrorDialogButtons buttons)
        {
            Debug.LogWarning("[Uta] error dialog: " + error + " buttons=" + buttons);
            if (autoDismiss)
            {
                return Fallback(buttons);
            }

            var dialogs = activeSceneUi.Dialogs;
            if (dialogs == null)
            {
                Debug.LogWarning("[Uta] error dialog: no scene ui");
                return Fallback(buttons);
            }

            string[] labels;
            switch (buttons)
            {
                case ErrorDialogButtons.Ok:
                    labels = new[] { "OK" };
                    break;
                case ErrorDialogButtons.RetryOrTitle:
                    labels = new[] { "再試行", "タイトルへ" };
                    break;
                case ErrorDialogButtons.Home:
                    labels = new[] { "ホームへ" };
                    break;
                case ErrorDialogButtons.Title:
                    labels = new[] { "タイトルへ" };
                    break;
                default:
                    labels = new[] { "ストアへ" };
                    break;
            }

            var index = await dialogs.ChooseAsync("エラーが発生しました。\n" + error, labels);
            if (index < 0)
            {
                return Fallback(buttons);
            }

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

        private static ErrorDialogButton Fallback(ErrorDialogButtons buttons)
        {
            switch (buttons)
            {
                case ErrorDialogButtons.Ok:
                    return ErrorDialogButton.Ok;
                case ErrorDialogButtons.RetryOrTitle:
                    return ErrorDialogButton.Title;
                case ErrorDialogButtons.Home:
                    return ErrorDialogButton.Home;
                case ErrorDialogButtons.Title:
                    return ErrorDialogButton.Title;
                default:
                    return ErrorDialogButton.Store;
            }
        }
    }
}
