using Cysharp.Threading.Tasks;
using UnityScreenNavigator;
using Uta.Client.Presentation.Startup;
using VContainer.Unity;

namespace Uta.Client.Presentation.Title
{
    /// <summary>Title シーンの入口。</summary>
    public sealed class TitleEntry : IStartable
    {
        private readonly IScreenNavigator navigator;
        private readonly StartupOptions options;
        private readonly TitleDiagnostics diagnostics;

        public TitleEntry(IScreenNavigator navigator, StartupOptions options, TitleDiagnostics diagnostics)
        {
            this.navigator = navigator;
            this.options = options;
            this.diagnostics = diagnostics;
        }

        public void Start()
        {
            StartAsync().Forget();
        }

        /// <summary>options.QuitAfterCheck なら診断を回す（画面を積まない）。そうでなければタイトルのページを積む。</summary>
        public async UniTask StartAsync()
        {
            if (options.QuitAfterCheck)
            {
                await diagnostics.RunAsync();
                return;
            }

            await navigator.PushPageAsync<TitlePresenter>(playAnimation: false);
        }
    }
}
