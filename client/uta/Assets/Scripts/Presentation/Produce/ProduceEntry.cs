using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityScreenNavigator;
using Uta.Client.Application.Produce;
using Uta.Client.Presentation.Common;
using VContainer.Unity;
using ISceneNavigator = Uta.Client.Application.Navigation.ISceneNavigator;

namespace Uta.Client.Presentation.Produce
{
    /// <summary>Produce シーンの入口。</summary>
    public sealed class ProduceEntry : IStartable
    {
        private readonly IScreenNavigator screenNavigator;
        private readonly ProduceSessionState produceSessionState;
        private readonly ProduceFlow produceFlow;
        private readonly DialogOpener dialogOpener;
        private readonly ISceneNavigator sceneNavigator;

        public ProduceEntry(IScreenNavigator screenNavigator, ProduceSessionState produceSessionState, ProduceFlow produceFlow, DialogOpener dialogOpener, ISceneNavigator sceneNavigator)
        {
            this.screenNavigator = screenNavigator;
            this.produceSessionState = produceSessionState;
            this.produceFlow = produceFlow;
            this.dialogOpener = dialogOpener;
            this.sceneNavigator = sceneNavigator;
        }

        public void Start()
        {
            StartAsync().Forget();
        }

        /// <summary>
        /// 進行中のセッションが無ければホームへ戻す。あれば、底の背景のページを積んで、現在の局面の画面を出す。
        /// 失敗したら知らせてホームへ戻る。
        /// </summary>
        public async UniTask StartAsync()
        {
            if (!produceSessionState.HasActive)
            {
                Debug.LogWarning("[Uta] produce: no active session");
                await sceneNavigator.GoToHomeAsync();
                return;
            }

            try
            {
                await screenNavigator.PushPageAsync<ProduceBackdropPresenter>(playAnimation: false);
                await produceFlow.ShowCurrentAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                await dialogOpener.ShowInfoAsync("エラーが発生しました: " + e.Message);
                await sceneNavigator.GoToHomeAsync();
            }
        }
    }
}
