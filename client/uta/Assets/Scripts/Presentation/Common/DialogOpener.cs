using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityScreenNavigator;

namespace Uta.Client.Presentation.Common
{
    /// <summary>
    /// 確認・お知らせ・選択のダイアログを開く。要求は FIFO で 1 つずつ処理する。
    /// テストで派生させる（FakeDialogOpener）ので sealed にせず、公開メソッドは virtual。
    /// USN の Overlay（IScreenNavigator.PushOverlayAsync）に DialogPresenter を出す。Overlay はページ遷移の最中にも出せる。
    /// </summary>
    public class DialogOpener : IDisposable
    {
        private readonly IScreenNavigator screenNavigator;
        private UniTask tail = UniTask.CompletedTask;   // 最後に積んだ要求が終わったら完了する
        private bool disposed;

        /// <summary>screenNavigator を保存するだけ（C# の null を許す。FakeDialogOpener が base(null) で作る）。</summary>
        public DialogOpener(IScreenNavigator screenNavigator)
        {
            this.screenNavigator = screenNavigator;
        }

        /// <summary>シーンのスコープの破棄で呼ばれる。待ち行列に残った要求は、順番が来ても出さずに −1 で終わる。</summary>
        public void Dispose()
        {
            disposed = true;
        }

        /// <summary>お知らせ。ボタンは「OK」1 つ（ButtonPrimary）。押されたら閉じて終わる。</summary>
        public virtual async UniTask ShowInfoAsync(string message)
        {
            await EnqueueAsync(message, new[] { "OK" }, new[] { UiTheme.ButtonPrimary });
        }

        /// <summary>確認。ボタンは上から positiveLabel（ButtonPrimary）→ Positive、negativeLabel（ButtonSecondary）→ Negative。</summary>
        public virtual async UniTask<DialogResult> ConfirmAsync(string message, string positiveLabel = "はい", string negativeLabel = "いいえ")
        {
            var index = await EnqueueAsync(
                message,
                new[] { positiveLabel, negativeLabel },
                new[] { UiTheme.ButtonPrimary, UiTheme.ButtonSecondary });
            return index == 0 ? DialogResult.Positive : DialogResult.Negative;
        }

        /// <summary>
        /// labels を上から並べ（全部 ButtonPrimary）、押されたボタンの添字（0 始まり）を返す。押されずにダイアログが消えたら（シーンを閉じた）−1。
        /// labels が null か、長さが 0 または 5 以上なら、待ち行列に入れる前に ArgumentException を投げる。
        /// async にしない（例外を呼んだその場で投げるため）。
        /// </summary>
        public virtual UniTask<int> ChooseAsync(string message, string[] labels)
        {
            if (labels == null || labels.Length == 0 || labels.Length >= 5)
            {
                throw new ArgumentException("labels must have 1 to 4 items", nameof(labels));
            }

            var colors = new Color[labels.Length];
            for (var i = 0; i < colors.Length; i++)
            {
                colors[i] = UiTheme.ButtonPrimary;
            }

            return EnqueueAsync(message, labels, colors);
        }

        private async UniTask<int> EnqueueAsync(string message, string[] labels, Color[] colors)
        {
            var previous = tail;
            var done = new UniTaskCompletionSource();
            tail = done.Task;                            // 次の要求は、この要求の done を待つ
            try
            {
                await previous;                          // 前の要求が終わるまで待つ（previous は done.Task なので例外にならない）
                if (disposed)
                {
                    return -1;                           // シーンを閉じたあとの待ち行列の残り
                }

                return await ShowOneAsync(message, labels, colors);
            }
            finally
            {
                done.TrySetResult();                     // 成功・例外を問わず、次の要求へ進める
            }
        }

        private async UniTask<int> ShowOneAsync(string message, string[] labels, Color[] colors)
        {
            if (screenNavigator == null)
            {
                throw new InvalidOperationException("screen navigator is not set");
            }

            var presenter = await screenNavigator.PushOverlayAsync<DialogPresenter, DialogArgs>(
                new DialogArgs(message, labels, colors),
                playAnimation: false);
            var result = await screenNavigator.WaitForPopAsync<object>(presenter);
            return result is int index ? index : -1;
        }
    }
}
