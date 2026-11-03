using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using Uta.Client.Application.Navigation;
using VContainer.Unity;

namespace Uta.Client.Bootstrap
{
    public sealed class SceneNavigator : ISceneNavigator
    {
        private readonly LifetimeScope root;

        public SceneNavigator(LifetimeScope root)
        {
            this.root = root;
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
            using (LifetimeScope.EnqueueParent(root))
            {
                await SceneManager.LoadSceneAsync(sceneName).ToUniTask();
            }
        }
    }
}
