using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Uta.Client.Presentation.Common;

namespace Uta.Client.Tests
{
    /// <summary>DialogOpener の偽。公開メソッドを全部上書きし、記録して積んだ値を返す（層を使わない）。</summary>
    public sealed class FakeDialogOpener : DialogOpener
    {
        private readonly CallLog log;

        public FakeDialogOpener(CallLog log)
            : base(null)
        {
            this.log = log;
        }

        /// <summary>ConfirmAsync が返す値。空なら DialogResult.Positive。</summary>
        public Queue<DialogResult> ConfirmResults { get; } = new Queue<DialogResult>();

        /// <summary>ChooseAsync が返す値。空なら 0。</summary>
        public Queue<int> ChooseResults { get; } = new Queue<int>();

        /// <summary>null でなければ、ShowInfoAsync はこの Task を待ってから終わる（実行中フラグのテスト用）。既定は null。</summary>
        public UniTaskCompletionSource InfoGate { get; set; }

        public override async UniTask ShowInfoAsync(string message)
        {
            log.Add("Info:" + message);
            if (InfoGate != null)
            {
                await InfoGate.Task;
            }
        }

        public override UniTask<DialogResult> ConfirmAsync(string message, string positiveLabel = "はい", string negativeLabel = "いいえ")
        {
            log.Add("Confirm:" + message);
            return UniTask.FromResult(ConfirmResults.Count > 0 ? ConfirmResults.Dequeue() : DialogResult.Positive);
        }

        public override UniTask<int> ChooseAsync(string message, string[] labels)
        {
            log.Add("Choose:" + message + ":" + string.Join("/", labels));
            return UniTask.FromResult(ChooseResults.Count > 0 ? ChooseResults.Dequeue() : 0);
        }
    }
}
