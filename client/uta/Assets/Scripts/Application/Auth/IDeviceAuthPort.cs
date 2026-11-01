using Cysharp.Threading.Tasks;

namespace Uta.Client.Application.Auth
{
    public interface IDeviceAuthPort
    {
        /// <summary>端末に保存したアカウント情報の状態。通信はしない。</summary>
        UniTask<AccountState> GetAccountStateAsync();

        /// <summary>有効なトークンを取る。未登録なら端末の登録（サインアップ）をしてから発行する。トークンは返さない。</summary>
        UniTask EnsureSignedInAsync();

        /// <summary>今のトークンを捨てて、発行し直す。</summary>
        UniTask ReissueTokenAsync();

        /// <summary>存在しない端末でトークンを求め、サーバーの認証エラー（Unauthenticated）が届けば true。</summary>
        UniTask<bool> CanReceiveServerErrorAsync();
    }
}
