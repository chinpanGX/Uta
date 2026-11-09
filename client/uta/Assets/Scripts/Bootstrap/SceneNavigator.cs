using System;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using Uta.Client.Application.Navigation;
using VContainer.Unity;

namespace Uta.Client.Bootstrap
{
    public sealed class SceneNavigator : ISceneNavigator
    {
        private readonly LifetimeScope lifetimeScope;
        private bool loading;

        public SceneNavigator(LifetimeScope lifetimeScope)
        {
            this.lifetimeScope = lifetimeScope;
        }

        public UniTask GoToTitleAsync()
        {
            return LoadAsync("Title");
        }

        public UniTask GoToHomeAsync()
        {
            return LoadAsync("Home");
        }

        public UniTask GoToProduceAsync()
        {
            return LoadAsync("Produce");
        }

        private async UniTask LoadAsync(string sceneName)
        {
            if (loading)
            {
                return;
            }

            loading = true;
            try
            {
                using (LifetimeScope.EnqueueParent(lifetimeScope))
                {
                    var operation = SceneManager.LoadSceneAsync(sceneName);
                    if (operation == null)
                    {
                        throw new InvalidOperationException("scene is not in build settings: " + sceneName);
                    }

                    await operation.ToUniTask();
                }
            }
            finally
            {
                loading = false;
            }
        }
    }
}
