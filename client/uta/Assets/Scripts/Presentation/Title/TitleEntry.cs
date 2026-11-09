using Cysharp.Threading.Tasks;
using UnityScreenNavigator;
using Uta.Client.Presentation.Startup;
using VContainer.Unity;

namespace Uta.Client.Presentation.Title
{
    /// <summary>Title シーンの入口。</summary>
    public sealed class TitleEntry : IStartable
    {
        private readonly IScreenNavigator screenNavigator;
        private readonly StartupOptions startupOptions;
        private readonly TitleDiagnostics titleDiagnostics;

        public TitleEntry(IScreenNavigator screenNavigator, StartupOptions startupOptions, TitleDiagnostics titleDiagnostics)
        {
            this.screenNavigator = screenNavigator;
            this.startupOptions = startupOptions;
            this.titleDiagnostics = titleDiagnostics;
        }

        public void Start()
        {
            StartAsync().Forget();
        }

        /// <summary>startupOptions.QuitAfterCheck なら診断を回す（画面を積まない）。そうでなければタイトルのページを積む。</summary>
        public async UniTask StartAsync()
        {
            if (startupOptions.QuitAfterCheck)
            {
                await titleDiagnostics.RunAsync();
                return;
            }

            await screenNavigator.PushPageAsync<TitlePresenter>(playAnimation: false);
        }
    }
}
