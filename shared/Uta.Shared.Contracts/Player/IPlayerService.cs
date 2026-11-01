using MagicOnion;

namespace Uta.Shared.Contracts.Player
{
    /// <summary>
    /// プレイヤーのサインアップとサインイン。認証あり・冪等性の対象。
    /// </summary>
    public interface IPlayerService : IService<IPlayerService>
    {
        UnaryResult<SignUpResponse> SignUpAsync();

        UnaryResult<SignInResponse> SignInAsync();
    }
}
