using Cysharp.Threading.Tasks;
using VolTech.Client;

namespace Uta.Client.Infrastructure.Api
{
    /// <summary>
    /// ITokenProvider を包み、Invalidate が呼ばれた回数を数える（再認証の再送の診断用）。
    /// ApiCore は UNAUTHENTICATED を受けるとトークンを捨てるために Invalidate を呼ぶ。
    /// </summary>
    internal sealed class CountingTokenProvider : ITokenProvider
    {
        private readonly ITokenProvider inner;

        public int InvalidationCount { get; private set; }

        public CountingTokenProvider(ITokenProvider inner)
        {
            this.inner = inner;
        }

        public UniTask<string> GetTokenAsync()
        {
            return inner.GetTokenAsync();
        }

        public void Invalidate(string rejectedToken)
        {
            InvalidationCount++;
            inner.Invalidate(rejectedToken);
        }

        public void Reset()
        {
            inner.Reset();
        }
    }
}
