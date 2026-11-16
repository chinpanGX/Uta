using Cysharp.Threading.Tasks;
using Uta.Client.Application.Produce;
using Uta.Client.Presentation.Common;

namespace Uta.Client.Presentation.Produce
{
    /// <summary>メニュー（[≡]）。テストで派生させるので sealed にせず、ShowAsync は virtual。例外は受けない（呼んだ Presenter のボタンの処理が受ける）。</summary>
    public class ProduceMenu
    {
        private readonly DialogOpener dialogOpener;
        private readonly IProduceUseCase produceUseCase;
        private readonly ProduceFlow produceFlow;

        public ProduceMenu(DialogOpener dialogOpener, IProduceUseCase produceUseCase, ProduceFlow produceFlow)
        {
            this.dialogOpener = dialogOpener;
            this.produceUseCase = produceUseCase;
            this.produceFlow = produceFlow;
        }

        public virtual async UniTask ShowAsync()
        {
            var index = await dialogOpener.ChooseAsync("メニュー", new[] { "ホームへ戻る（あとで再開できます）", "プロデュースを放棄する", "閉じる" });
            if (index == 0)
            {
                await produceFlow.ExitToHomeAsync();
            }
            else if (index == 1)
            {
                var answer = await dialogOpener.ConfirmAsync("放棄すると報酬は受け取れません。放棄しますか？");
                if (answer == DialogResult.Positive && await produceUseCase.AbandonAsync())
                {
                    await produceFlow.ExitToHomeAsync();
                }
            }
        }
    }
}
