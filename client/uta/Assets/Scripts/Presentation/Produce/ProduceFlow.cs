using System;
using Cysharp.Threading.Tasks;
using UnityScreenNavigator;
using ISceneNavigator = Uta.Client.Application.Navigation.ISceneNavigator;
using Uta.Client.Application.Produce;
using Uta.Client.Presentation.Common;
using Uta.Shared.Domain.Produce;

namespace Uta.Client.Presentation.Produce
{
    /// <summary>
    /// 局面から画面への振り分け。Produce シーンのスコープに登録する。テストで派生させるので sealed にせず、公開メソッドは virtual。
    /// 局面の画面の Presenter が ShowCurrentAsync・HandleStepAsync・ExitToHomeAsync を呼ぶと、その Presenter 自身が閉じられる。呼んだ後は View に触らない。
    /// </summary>
    public class ProduceFlow
    {
        private readonly ProduceSessionState produceSessionState;
        private readonly IScreenNavigator screenNavigator;
        private readonly PageStack pageStack;
        private readonly DialogOpener dialogOpener;
        private readonly ISceneNavigator sceneNavigator;

        public ProduceFlow(ProduceSessionState produceSessionState, IScreenNavigator screenNavigator, PageStack pageStack, DialogOpener dialogOpener, ISceneNavigator sceneNavigator)
        {
            this.produceSessionState = produceSessionState;
            this.screenNavigator = screenNavigator;
            this.pageStack = pageStack;
            this.dialogOpener = dialogOpener;
            this.sceneNavigator = sceneNavigator;
        }

        /// <summary>
        /// session.Current の局面の画面を出す。Current が null なら InvalidOperationException("no active produce")。
        /// 底の背景のページより上のページ・モーダルを全部閉じてから、局面の画面を積む。
        /// </summary>
        public virtual async UniTask ShowCurrentAsync()
        {
            if (produceSessionState.Current == null)
            {
                throw new InvalidOperationException("no active produce");
            }

            await pageStack.PopToRootAsync(screenNavigator);
            await PushForPhaseAsync(produceSessionState.Current.Phase);
        }

        /// <summary>プロデュースの画面を離れてホームへ戻る。session も所持品の取り直しも触らない。</summary>
        public virtual UniTask ExitToHomeAsync()
        {
            return sceneNavigator.GoToHomeAsync();
        }

        /// <summary>Succeeded・Recovered は次の画面、SessionEnded は知らせてホームへ、Rejected は知らせるだけ、Failed は何もしない。</summary>
        public virtual async UniTask HandleStepAsync(ProduceStepResultKind kind)
        {
            switch (kind)
            {
                case ProduceStepResultKind.Succeeded:
                case ProduceStepResultKind.Recovered:
                    await ShowCurrentAsync();
                    break;
                case ProduceStepResultKind.SessionEnded:
                    await dialogOpener.ShowInfoAsync("プロデュースは終了しています。");
                    await ExitToHomeAsync();
                    break;
                case ProduceStepResultKind.Rejected:
                    await dialogOpener.ShowInfoAsync("その操作はできません。");
                    break;
                case ProduceStepResultKind.Failed:
                    break;
            }
        }

        /// <summary>局面ごとの画面を積む（底の背景のページの上に）。画面がまだ無い局面は、知らせてホームへ戻る（セッションはサーバーに残る）。</summary>
        protected virtual async UniTask PushForPhaseAsync(ProducePhase phase)
        {
            switch (phase)
            {
                default:
                    await dialogOpener.ShowInfoAsync("この局面の画面は準備中です: " + phase);
                    await ExitToHomeAsync();
                    return;
            }
        }
    }
}
