using Cysharp.Threading.Tasks;
using UnityEngine;
using Uta.Client.Application.Navigation;
using VolTech.Client;

namespace Uta.Client.Bootstrap
{
    public sealed class ErrorNavigator : IErrorNavigator
    {
        private readonly ISceneNavigator scenes;

        public ErrorNavigator(ISceneNavigator scenes)
        {
            this.scenes = scenes;
        }

        public UniTask GoToTitleAsync()
        {
            return scenes.GoToTitleAsync();
        }

        public UniTask GoToHomeAsync()
        {
            return scenes.GoToHomeAsync();
        }

        public UniTask OpenStoreAsync()
        {
            Debug.LogWarning("[Uta] OpenStore is not implemented");
            return UniTask.CompletedTask;
        }
    }
}
