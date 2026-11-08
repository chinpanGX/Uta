using Cysharp.Threading.Tasks;
using UnityScreenNavigator;
using Uta.Client.Domain.Player;
using VContainer.Unity;
using ISceneNavigator = Uta.Client.Application.Navigation.ISceneNavigator;

namespace Uta.Client.Presentation.Home
{
    /// <summary>Home シーンの入口。</summary>
    public sealed class HomeEntry : IStartable
    {
        private readonly IScreenNavigator navigator;
        private readonly IReadOnlyPlayerRepository players;
        private readonly ISceneNavigator scenes;

        public HomeEntry(IScreenNavigator navigator, IReadOnlyPlayerRepository players, ISceneNavigator scenes)
        {
            this.navigator = navigator;
            this.players = players;
            this.scenes = scenes;
        }

        public void Start()
        {
            StartAsync().Forget();
        }

        /// <summary>サインインしていなければ Title へ戻す（screen-design 8章）。そうでなければホームのページを積む。</summary>
        public async UniTask StartAsync()
        {
            if (players.Find() == null)
            {
                await scenes.GoToTitleAsync();
                return;
            }

            await navigator.PushPageAsync<HomePresenter>(playAnimation: false);
        }
    }
}
