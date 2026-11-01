namespace Uta.Client.Application.Auth
{
    /// <summary>端末に保存したアカウント情報の状態。</summary>
    public enum AccountState
    {
        NotRegistered = 0,
        Registered = 1,
        Corrupted = 2,
    }
}
