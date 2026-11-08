using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityScreenNavigator;

namespace Uta.Client.Tests
{
    /// <summary>IScreenNavigator の偽。どれも記録して同期で終わる。</summary>
    public sealed class FakeScreenNavigator : IScreenNavigator
    {
        private readonly CallLog log;

        public FakeScreenNavigator(CallLog log)
        {
            this.log = log;
        }

        /// <summary>Push に渡された Args（渡された順）。</summary>
        public List<object> Args { get; } = new List<object>();

        /// <summary>WaitForPopAsync が返す値。空なら default(TResult)。</summary>
        public Queue<object> PopResults { get; } = new Queue<object>();

        /// <summary>空でなければ、WaitForPopAsync は先頭の Source が終わるまで待ち、その値を返す（待ち行列のテスト用）。</summary>
        public Queue<UniTaskCompletionSource<object>> PopGates { get; } = new Queue<UniTaskCompletionSource<object>>();

        public UniTask<TPresenter> PushPageAsync<TPresenter>(bool stack = true, bool playAnimation = true)
            where TPresenter : IPresenter
        {
            log.Add("PushPage:" + typeof(TPresenter).Name + (stack ? string.Empty : "(nostack)"));
            return UniTask.FromResult(default(TPresenter));
        }

        public UniTask<TPresenter> PushPageAsync<TPresenter, TArgs>(TArgs args, bool stack = true, bool playAnimation = true)
            where TPresenter : IPresenter, IScreenWithArgs<TArgs>
            where TArgs : class
        {
            log.Add("PushPage:" + typeof(TPresenter).Name + (stack ? string.Empty : "(nostack)"));
            Args.Add(args);
            return UniTask.FromResult(default(TPresenter));
        }

        public UniTask PopPageAsync(bool playAnimation = true, int popCount = 1)
        {
            log.Add("PopPage:" + popCount);
            return UniTask.CompletedTask;
        }

        public UniTask PopPageAsync(IPresenter presenter, bool playAnimation = true)
        {
            log.Add("PopPage:" + NameOf(presenter));
            return UniTask.CompletedTask;
        }

        public UniTask<TPresenter> PushModalAsync<TPresenter>(bool playAnimation = true)
            where TPresenter : IPresenter
        {
            log.Add("PushModal:" + typeof(TPresenter).Name);
            return UniTask.FromResult(default(TPresenter));
        }

        public UniTask<TPresenter> PushModalAsync<TPresenter, TArgs>(TArgs args, bool playAnimation = true)
            where TPresenter : IPresenter, IScreenWithArgs<TArgs>
            where TArgs : class
        {
            log.Add("PushModal:" + typeof(TPresenter).Name);
            Args.Add(args);
            return UniTask.FromResult(default(TPresenter));
        }

        public UniTask PopModalAsync(bool playAnimation = true, int popCount = 1)
        {
            log.Add("PopModal:" + popCount);
            return UniTask.CompletedTask;
        }

        public UniTask PopModalAsync(IPresenter presenter, bool playAnimation = true)
        {
            log.Add("PopModal:" + NameOf(presenter));
            return UniTask.CompletedTask;
        }

        public UniTask<TPresenter> PushOverlayAsync<TPresenter>(bool playAnimation = true)
            where TPresenter : IPresenter
        {
            log.Add("PushOverlay:" + typeof(TPresenter).Name);
            return UniTask.FromResult(default(TPresenter));
        }

        public UniTask<TPresenter> PushOverlayAsync<TPresenter, TArgs>(TArgs args, bool playAnimation = true)
            where TPresenter : IPresenter, IScreenWithArgs<TArgs>
            where TArgs : class
        {
            log.Add("PushOverlay:" + typeof(TPresenter).Name);
            Args.Add(args);
            return UniTask.FromResult(default(TPresenter));
        }

        public UniTask PopOverlayAsync(bool playAnimation = true, int popCount = 1)
        {
            log.Add("PopOverlay:" + popCount);
            return UniTask.CompletedTask;
        }

        public UniTask PopOverlayAsync(IPresenter presenter, bool playAnimation = true)
        {
            log.Add("PopOverlay:" + NameOf(presenter));
            return UniTask.CompletedTask;
        }

        public UniTask<TResult> WaitForPopAsync<TResult>(IPresenter presenter, CancellationToken cancellation = default)
        {
            log.Add("WaitForPop");
            if (PopGates.Count > 0)
            {
                return WaitGateAsync<TResult>(PopGates.Dequeue());
            }

            var result = PopResults.Count > 0 ? (TResult)PopResults.Dequeue() : default(TResult);
            return UniTask.FromResult(result);
        }

        private static async UniTask<TResult> WaitGateAsync<TResult>(UniTaskCompletionSource<object> gate)
        {
            return (TResult)await gate.Task;
        }

        private static string NameOf(IPresenter presenter)
        {
            return presenter == null ? "null" : presenter.GetType().Name;
        }
    }
}
