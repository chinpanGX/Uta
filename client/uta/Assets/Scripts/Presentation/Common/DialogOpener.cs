using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Presentation.Common
{
    /// <summary>
    /// 確認・お知らせ・選択のダイアログを開く。要求は FIFO で 1 つずつ処理する。
    /// テストで派生させる（FakeDialogOpener）ので sealed にせず、公開メソッドは virtual。
    /// USN のモーダルにしない（ページ遷移の最中にも出せるように、USN の遷移の外の素の層（UiRoot.DialogLayer）に出す）。
    /// </summary>
    public class DialogOpener
    {
        private readonly RectTransform layer;
        private readonly DialogView prefab;
        private UniTask tail = UniTask.CompletedTask;   // 最後に積んだ要求が終わったら完了する

        /// <summary>layer・prefab を保存するだけ（C# の null を許す。FakeDialogOpener が base(null, null) で作る）。</summary>
        public DialogOpener(RectTransform layer, DialogView prefab)
        {
            this.layer = layer;
            this.prefab = prefab;
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
                if (IsLayerDestroyed())
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

        // C# の null（コンストラクターに null を渡した）は「破棄済み」に数えない（ShowOneAsync が InvalidOperationException を投げる）。
        // Unity の null（GameObject が破棄済み）だけを破棄済みとする
        private bool IsLayerDestroyed()
        {
            return !ReferenceEquals(layer, null) && layer == null;
        }

        private UniTask<int> ShowOneAsync(string message, string[] labels, Color[] colors)
        {
            if (ReferenceEquals(layer, null) || ReferenceEquals(prefab, null))
            {
                throw new InvalidOperationException("dialog layer is not set");
            }

            var source = new UniTaskCompletionSource<int>();
            var finished = false;
            var view = UnityEngine.Object.Instantiate(prefab, layer, false);
            view.gameObject.name = "Dialog";
            view.SetMessage(message);

            void Complete(int index)
            {
                if (finished)
                {
                    return;
                }

                finished = true;
                UiObjects.Destroy(view.gameObject);
                source.TrySetResult(index);
            }

            view.Destroyed += () =>
            {
                if (!finished)
                {
                    finished = true;
                    source.TrySetResult(-1);
                }
            };

            for (var i = 0; i < labels.Length; i++)
            {
                var index = i;
                var button = view.AddButton("Button" + i, labels[i], colors[i]);
                button.Button.onClick.AddListener(() => Complete(index));
            }

            return source.Task;
        }
    }
}
