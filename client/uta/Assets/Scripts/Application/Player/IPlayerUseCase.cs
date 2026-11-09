using Cysharp.Threading.Tasks;

namespace Uta.Client.Application.Player
{
    public interface IPlayerUseCase
    {
        /// <summary>
        /// afterRegistration が true（端末を登録した直後）なら、先にサインアップ → サインイン。
        /// サインインが PLAYER_NOT_CREATED なら、まだサインアップしていなければサインアップしてサインインし直す。
        /// サインアップの後も未作成なら InvalidOperationException("sign-in: player not created after sign-up")。
        /// 失敗は InvalidOperationException（"sign-up failed: " + 失敗の文字列、"sign-in failed: " + 失敗の文字列）。
        /// 成功した応答は 3 つのリポジトリに書いてから返す。失敗したらリポジトリは変えない。
        /// 応答の PlayerId・アイドルの UniqueId・サポートカードの UniqueId が null なら ArgumentNullException（リポジトリは変えない）。
        /// </summary>
        UniTask<EnterPlayerResult> EnterAsync(bool afterRegistration);

        /// <summary>
        /// サインインで所持データを取り直す。成功で 3 つのリポジトリを置き換える。
        /// 失敗は InvalidOperationException（"sign-in failed: " + 失敗の文字列。未作成は "sign-in failed: player not created"）。リポジトリは変えない。
        /// 応答の ID が null のときは EnterAsync と同じく ArgumentNullException（リポジトリは変えない）。
        /// </summary>
        UniTask RefreshAsync();

        /// <summary>破棄の知らせを出したと記録する（DiscardNoticeState.MarkShown）。通信しない。</summary>
        void MarkDiscardNoticeShown();
    }
}
