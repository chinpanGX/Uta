using Cysharp.Threading.Tasks;
using Uta.Client.Application.Navigation;

namespace Uta.Client.Tests
{
    /// <summary>ISceneNavigator の偽。どれも記録して同期で終わる。</summary>
    public sealed class FakeSceneNavigator : ISceneNavigator
    {
        private readonly CallLog log;

        public FakeSceneNavigator(CallLog log)
        {
            this.log = log;
        }

        public UniTask GoToTitleAsync()
        {
            log.Add("GoToTitle");
            return UniTask.CompletedTask;
        }

        public UniTask GoToHomeAsync()
        {
            log.Add("GoToHome");
            return UniTask.CompletedTask;
        }

        public UniTask GoToProduceAsync()
        {
            log.Add("GoToProduce");
            return UniTask.CompletedTask;
        }
    }
}
