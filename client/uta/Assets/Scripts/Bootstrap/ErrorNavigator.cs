using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Uta.Client.Application.Navigation;
using Uta.Client.Application.Player;
using VContainer;
using VolTech.Client;

namespace Uta.Client.Bootstrap
{
    public sealed class ErrorNavigator : IErrorNavigator
    {
        private readonly ISceneNavigator scenes;
        private readonly IObjectResolver resolver;
        private bool refreshing;

        // IPlayerUseCase をコンストラクターで受けない:
        // ApiClient のファクトリー → IErrorNavigator → IPlayerUseCase → IPlayerService → ApiClient の循環になるため
        public ErrorNavigator(ISceneNavigator scenes, IObjectResolver resolver)
        {
            this.scenes = scenes;
            this.resolver = resolver;
        }

        public UniTask GoToTitleAsync()
        {
            return scenes.GoToTitleAsync();
        }

        public async UniTask GoToHomeAsync()
        {
            if (!refreshing)
            {
                refreshing = true;
                try
                {
                    await resolver.Resolve<IPlayerUseCase>().RefreshAsync();
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Uta] refresh on go-to-home failed: " + e.Message);
                }
                finally
                {
                    refreshing = false;
                }
            }

            await scenes.GoToHomeAsync();
        }

        public UniTask OpenStoreAsync()
        {
            Debug.LogWarning("[Uta] OpenStore is not implemented");
            return UniTask.CompletedTask;
        }
    }
}
