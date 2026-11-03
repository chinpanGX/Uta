using Cysharp.Threading.Tasks;

namespace Uta.Client.Application.Player
{
    public interface IPlayerPort
    {
        /// <summary>プレイヤーを作る（2 回目以降は何もしない）。通信の失敗は例外にせず Failure で返す。</summary>
        UniTask<PlayerSignUpResult> SignUpAsync();

        /// <summary>所持データを全件取る。プレイヤー未作成は NotCreated、そのほかの失敗は Failure で返す。</summary>
        UniTask<PlayerSignInResult> SignInAsync();
    }
}
