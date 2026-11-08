using Cysharp.Threading.Tasks;

namespace Uta.Client.Application.Auth
{
    /// <summary>端末の認証。Contracts に相手が無い機能なので、UseCase の IF に通信の操作を含める（ADR 0022）。</summary>
    public interface IDeviceAuthUseCase
    {
        /// <summary>
        /// 端末の認証。アカウントの状態が Corrupted なら InvalidOperationException("account corrupted")、
        /// 知らない値なら InvalidOperationException("unknown account status: " + status)。
        /// トークンを取り（未登録なら端末を登録して発行する）、先に取った状態が NotRegistered だったなら true（初めて登録した）を返す。
        /// </summary>
        UniTask<bool> SignInAsync();

        /// <summary>今のトークンを捨てて、発行し直す。</summary>
        UniTask ReissueTokenAsync();

        // 診断（ConnectivityCheck）が使う 3 つ。

        /// <summary>存在しない端末でトークンを求め、サーバーの認証エラー（Unauthenticated）が届けば true。</summary>
        UniTask<bool> CanReceiveServerErrorAsync();

        /// <summary>別のトークンの提供者で発行し直し、いまこの端末が持つトークンをサーバー側で無効にする。</summary>
        UniTask InvalidateServerSideTokenAsync();

        /// <summary>ApiCore がトークンを捨てた回数。</summary>
        int TokenInvalidationCount { get; }
    }
}
